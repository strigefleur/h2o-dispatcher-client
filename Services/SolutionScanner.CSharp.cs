using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using Ardalis.GuardClauses;
using Felweed.Models;
using Serilog;

namespace Felweed.Services;

public static partial class SolutionScanner
{
    private static async IAsyncEnumerable<CSharpSolution> ScanCSharpSolutionsAsync(
        IEnumerable<string> directories,
        ICollection<string>? allowedPrefixes,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<string>();
    
        // Producer: find all .sln files
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var dir in directories)
                {
                    foreach (var file in dir.EnumerateFilesWithExclusions(allowedPrefixes, "*.sln"))
                    {
                        await channel.Writer.WriteAsync(file, ct);
                    }
                }
            }
            finally { channel.Writer.Complete(); }
        }, ct);

        // Consumer: parse and yield
        await foreach (var file in channel.Reader.ReadAllAsync(ct))
        {
            yield return await ParseCSharpSolutionAsync(file, ct);
        }
    }

    private static async Task<CSharpSolution> ParseCSharpSolutionAsync(string slnPath, CancellationToken ct = default)
    {
        var produces = new List<string>();
        var dependencies = new List<ConsumedDependency>();
        var slnDir = Path.GetDirectoryName(slnPath);

        // Find all .csproj files referenced in solution
        var projects = GetProjectsFromSolution(slnPath, slnDir);
            
        bool? isCorporate = null;
        foreach (var csproj in projects.Where(File.Exists))
        {
            var (packable, deps) = ParseCsprojDependencies(csproj);
            
            dependencies.AddRange(deps);
            if (packable)
            {
                var doc = XDocument.Load(csproj);
                var packageId = GetPackageId(doc);
                
                produces.Add(packageId);
            }

            if (isCorporate == null && !csproj.EndsWith("Tests.csproj"))
            {
                var doc = XDocument.Load(csproj);
                var packageId = GetPackageId(doc);

                isCorporate = packageId.StartsWith(Constants.PrefixConst.CSharpCorporateL0Prefix);
            }
        }
        
        var nugetPackageNamePart = Path.GetFileName(slnPath)
            .Replace(".sln", string.Empty)
            .Replace("-", string.Empty);
        
        var (originUrl, tagVersion) = GitHelper.GetRepoInfo(slnDir);
        
        var solution = new CSharpSolution
        {
            Name = slnDir.Split(Path.DirectorySeparatorChar).Last(),
            Path = slnPath,
            PackageId = $"{Constants.PrefixConst.CSharpCorporateL0Prefix}.{nugetPackageNamePart}",
            Type = GitlabConfigHelper.GetProjectType(Path.Combine(slnDir, ".gitlab-ci.yml")),
            TagVersionNumber = tagVersion,
            GitOriginUrl = originUrl,
            LatestSyncDate = GitHelper.GetLastGitSyncDate(slnDir),
            IsCorporate = isCorporate
        };
        
        solution.AddConsumedDependencies([..dependencies.OrderBy(d => d.Name).ThenBy(x => x.Version)]);
        
        solution.AddProducedDependencies([..produces]);

        return solution;
    }

    private static List<string> GetProjectsFromSolution(string slnPath, string slnDir)
    {
        var content = File.ReadAllText(slnPath);
        return ProjectRegex()
            .Matches(content)
            .Select(m => Path.GetFullPath(Path.Combine(slnDir, m.Groups[1].Value)))
            .Where(p => p.EndsWith(".csproj"))
            .ToList();
    }

    private static (bool Packable, List<ConsumedDependency> Deps) ParseCsprojDependencies(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            return (IsPackable(doc),
                    doc.Descendants()
                        .Where(e => e.Name.LocalName == "PackageReference")
                        .Select(e =>
                        {
                            var name = e.Attribute("Include")?.Value ?? "Unknown";
                            var version = e.Attribute("Version")?.Value ?? e.Element(ns + "Version")?.Value ?? "*";

                            return new ConsumedDependency(name, version);
                        })
                        .ToList()
                );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse dependencies");
            return (false, []);
        }
    }
    
    private static string GetPackageId(XDocument xdoc)
    {
        return Guard.Against.NullOrWhiteSpace(ReadFirstProperty(xdoc, "PackageId"));
    }
    
    private static bool IsPackable(XDocument xdoc)
    {
        var isPackable = ReadFirstProperty(xdoc, "IsPackable");

        return string.Equals(isPackable, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadFirstProperty(XDocument xdoc, string name) =>
        xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value?.Trim();

    [GeneratedRegex(@"Project\([^)]+\)\s*=\s*""[^""]*""\s*,\s*""([^""]+)""")]
    private static partial Regex ProjectRegex();
}