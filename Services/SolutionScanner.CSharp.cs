using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using Felweed.Models;

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
        var dependencies = new List<CSharpSolutionDependency>();
        var slnDir = Path.GetDirectoryName(slnPath);

        // Find all .csproj files referenced in solution
        var projects = GetProjectsFromSolution(slnPath, slnDir);

        foreach (var csproj in projects.Where(File.Exists))
        {
            dependencies.AddRange(ParseCsprojDependencies(csproj));
        }

        var solution = new CSharpSolution
        {
            Name = slnDir.Split(Path.DirectorySeparatorChar).Last(),
            Path = slnPath,
            Type = GitlabConfigHelper.GetProjectType(Path.Combine(slnDir, ".gitlab-ci.yml")),
            ChangelogVersionNumber =
                await ChangelogHelper.GetLatestVersionNumberAsync(Path.Combine(slnDir, "CHANGELOG.md"), ct),
            LatestSyncDate = GitHelper.GetLastGitSyncDate(slnDir),
        };
        solution.AddDependencyRange([.. dependencies.Distinct().OrderBy(d => d.Name)]);

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

    private static List<CSharpSolutionDependency> ParseCsprojDependencies(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            return doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e =>
                {
                    var name = e.Attribute("Include")?.Value ?? "Unknown";
                    var version = e.Attribute("Version")?.Value ?? e.Element(ns + "Version")?.Value ?? "*";

                    return new CSharpSolutionDependency(name, version);
                })
                .ToList();
        }
        catch { return []; }
    }

    [GeneratedRegex(@"Project\([^)]+\)\s*=\s*""[^""]*""\s*,\s*""([^""]+)""")]
    private static partial Regex ProjectRegex();
}