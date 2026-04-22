using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Ardalis.GuardClauses;
using Felweed.Models;
using Felweed.Models.Enumerators;
using Serilog;

namespace Felweed.Services;

public static partial class SolutionScanner
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
            finally
            {
                channel.Writer.Complete();
            }
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
        var (name, dependencies) = File.Exists(packageJson)
            ? ParsePackageJson(packageJson)
            : ("NotExists", []);

        var (originUrl, tagVersion) = GitHelper.GetRepoInfo(angularDir);

        var solution = new AngularSolution
        {
            Name = angularDir.Split(Path.DirectorySeparatorChar).Last(),
            Path = angularDir,
            PackageId = name,
            Type = GitlabConfigHelper.GetProjectType(Path.Combine(angularDir, ".gitlab-ci.yml")),
            GitOriginUrl = originUrl,
            LatestSyncDate = GitHelper.GetLastGitSyncDate(angularDir),
            IsCorporate = name.StartsWith(Constants.PrefixConst.AngularCorporateL0Prefix)
        };
        
        solution.UpdateTagVersionNumber(tagVersion);

        solution.AddConsumedDependencies(dependencies.ToArray());

        if (solution.Type == SolutionType.Library)
            solution.AddProducedDependencies(name);

        return solution;
    }

    private static (string Name, List<ConsumedDependency> Deps) ParsePackageJson(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown";
            var version = root.TryGetProperty("version", out var versionEl) ? versionEl.GetString() : "?.?.?";

            var deps = new List<ConsumedDependency>();

            deps.AddRange(ExtractDeps(root, "dependencies", AngularDependencyType.Runtime));
            deps.AddRange(ExtractDeps(root, "devDependencies", AngularDependencyType.Dev));
            deps.AddRange(ExtractDeps(root, "peerDependencies", AngularDependencyType.Peer));

            return (name, [.. deps.OrderBy(d => d.Name).ThenBy(x => x.Version)]);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse package.json");
            return ("Error", []);
        }
    }

    private static IEnumerable<ConsumedDependency> ExtractDeps(JsonElement root, string section,
        AngularDependencyType type)
    {
        if (!root.TryGetProperty(section, out var element))
            yield break;

        foreach (var prop in element.EnumerateObject())
            yield return new ConsumedDependency(Name: prop.Name, Version: prop.Value.GetString() ?? "*");
    }
}