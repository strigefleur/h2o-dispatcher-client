using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Felweed.Constants;
using Felweed.Models;
using Felweed.Models.Enumerators;

namespace Felweed.Services.Graph;

public static partial class CorporateGraphBuilder
{
    public static CorporateGraph Build(SolutionScanner scanner)
    {
        var allSolutions = scanner.CsharpSolutions.Cast<Solution>()
            .Concat(scanner.AngularSolutions)
            .ToList();

        // Build "package name -> producing solutions" index
        var producersByPackage = new Dictionary<string, List<Solution>>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in allSolutions.Where(x => x.Type == SolutionType.Library))
        {
            foreach (var pkg in GetProducedCorporatePackages(s))
            {
                if (!producersByPackage.TryGetValue(pkg, out var list))
                {
                    list = [];
                    producersByPackage[pkg] = list;
                }
                list.Add(s);
            }
        }

        // Build edges from corporate direct deps
        var edges = new List<CorporateEdge>();

        foreach (var s in allSolutions)
        {
            IEnumerable<string> corporateDeps = s switch
            {
                CSharpSolution cs => cs.Dependencies.Where(d => d.Type == SolutionDependencyType.Corporate).Select(d => d.Name),
                AngularSolution ng => ng.Dependencies.Where(d => d.Type == SolutionDependencyType.Corporate).Select(d => d.Name),
                _ => Enumerable.Empty<string>()
            };

            foreach (var depName in corporateDeps.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!producersByPackage.TryGetValue(depName, out var producers))
                    continue; // corporate package exists but producer isn't in scanned set

                foreach (var producer in producers)
                {
                    if (producer.Path.Equals(s.Path, StringComparison.OrdinalIgnoreCase))
                        continue;

                    edges.Add(new CorporateEdge
                    {
                        From = s,
                        To = producer,
                        PackageName = depName
                    });
                }
            }
        }

        // de-dup edges
        edges = edges
            .DistinctBy(e => (e.From.Path, e.To.Path, e.PackageName))
            .ToList();

        return new CorporateGraph
        {
            Nodes = allSolutions,
            Edges = edges
        };
    }

    private static IEnumerable<string> GetProducedCorporatePackages(Solution s)
    {
        return s switch
        {
            AngularSolution ng => GetAngularProducedPackages(ng),
            CSharpSolution cs => GetDotNetProducedPackages(cs),
            _ => Enumerable.Empty<string>()
        };
    }

    private static IEnumerable<string> GetAngularProducedPackages(AngularSolution s)
    {
        var packageJsonPath = Path.Combine(s.Path, "package.json");
        if (!File.Exists(packageJsonPath))
            yield break;

        using var doc = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
        if (!doc.RootElement.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
            yield break;

        var name = nameEl.GetString()!;
        if (name.StartsWith(PrefixConst.AngularCorporateDepPrefix, StringComparison.OrdinalIgnoreCase))
            yield return name;
    }

    private static IEnumerable<string> GetDotNetProducedPackages(CSharpSolution s)
    {
        var slnPath = s.Path;
        var slnDir = Path.GetDirectoryName(slnPath)!;

        foreach (var csproj in GetProjectsFromSolution(slnPath, slnDir))
        {
            if (!File.Exists(csproj)) continue;

            XDocument doc;
            try { doc = XDocument.Load(csproj); }
            catch { continue; }

            if (!IsPackable(doc))
                continue;

            var packageId = ReadFirstProperty(doc, "PackageId")
                            ?? ReadFirstProperty(doc, "AssemblyName")
                            ?? Path.GetFileNameWithoutExtension(csproj);

            if (packageId.StartsWith(PrefixConst.CSharpCorporateDepPrefix, StringComparison.OrdinalIgnoreCase))
                yield return packageId;
        }
    }

    private static bool IsPackable(XDocument xdoc)
    {
        var isPackable = ReadFirstProperty(xdoc, "IsPackable");
        var pack = ReadFirstProperty(xdoc, "Pack");

        if (string.Equals(isPackable, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(pack, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (ReadFirstProperty(xdoc, "PackageId") is not null) return true;

        return false;
    }

    private static string? ReadFirstProperty(XDocument xdoc, string name) =>
        xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value?.Trim();

    private static List<string> GetProjectsFromSolution(string slnPath, string slnDir)
    {
        var content = File.ReadAllText(slnPath);
        return ProjectRegex()
            .Matches(content)
            .Select(m => Path.GetFullPath(Path.Combine(slnDir, m.Groups[1].Value)))
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [GeneratedRegex(@"Project$$[^)]+$$\s*=\s*""[^""]*""\s*,\s*""([^""]+)""")]
    private static partial Regex ProjectRegex();
}