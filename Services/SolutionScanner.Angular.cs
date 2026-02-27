using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Ardalis.GuardClauses;
using Felweed.Models;
using Felweed.Models.Enumerators;

namespace Felweed.Services;

public partial class SolutionScanner
{
    private static async IAsyncEnumerable<AngularSolution> ScanAngularSolutionsAsync(
        IEnumerable<string> directories,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<string>();
    
        // Producer: find all angular.json files
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var dir in directories)
                {
                    foreach (var file in dir.EnumerateFilesWithExclusions(null, "angular.json"))
                    {
                        await channel.Writer.WriteAsync(Guard.Against.Null(Path.GetDirectoryName(file)), ct);
                    }
                }
            }
            finally { channel.Writer.Complete(); }
        }, ct);

        // Consumer: parse and yield
        await foreach (var path in channel.Reader.ReadAllAsync(ct))
        {
            yield return await ParseAngularSolutionAsync(path, ct);
        }
    }

    private static async Task<AngularSolution> ParseAngularSolutionAsync(string angularDir,
        CancellationToken ct = default)
    {
        var packageJson = Path.Combine(angularDir, "package.json");
        var dependencies = File.Exists(packageJson)
            ? ParsePackageJson(packageJson)
            : [];

        var solution = new AngularSolution
        {
            Name = angularDir.Split(Path.DirectorySeparatorChar).Last(),
            Path = angularDir,
            Type = GitlabConfigHelper.GetProjectType(Path.Combine(angularDir, ".gitlab-ci.yml")),
            ChangelogVersionNumber =
                await ChangelogHelper.GetLatestVersionNumberAsync(Path.Combine(angularDir, "CHANGELOG.md"), ct),
            LatestSyncDate = GitHelper.GetLastGitSyncDate(angularDir),
        };
        solution.AddDependencyRange(dependencies.ToArray());

        return solution;
    }

    private static List<AngularSolutionDependency> ParsePackageJson(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            
            var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "Unknown";
            var version = root.TryGetProperty("version", out var versionEl) ? versionEl.GetString() : "?.?.?";
            
            var deps = new List<AngularSolutionDependency>();

            deps.AddRange(ExtractDeps(root, "dependencies", AngularDependencyType.Runtime));
            deps.AddRange(ExtractDeps(root, "devDependencies", AngularDependencyType.Dev));
            deps.AddRange(ExtractDeps(root, "peerDependencies", AngularDependencyType.Peer));

            return [.. deps.OrderBy(d => d.Name)];
        }
        catch { return []; }
    }
    
    private static IEnumerable<AngularSolutionDependency> ExtractDeps(JsonElement root, string section, AngularDependencyType type)
    {
        if (!root.TryGetProperty(section, out var element)) 
            yield break;

        foreach (var prop in element.EnumerateObject())
            yield return new AngularSolutionDependency(prop.Name, prop.Value.GetString() ?? "*", type);
    }
}