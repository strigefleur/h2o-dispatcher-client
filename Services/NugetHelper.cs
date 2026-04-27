using System.IO;
using Felweed.Models;
using NuGet.Configuration;
using Serilog;

namespace Felweed.Services;

public static class NugetHelper
{
    public static bool IsValidNugetFeedConfig()
    {
        var feedConfig = ConfigurationService.ReadNugetFeedConfig();
        if (!feedConfig.IsValid())
            return false;

        if (ShouldRequestNugetData(feedConfig.ConfigPath, feedConfig.Name))
            return false;

        return true;
    }

    private static ISettings LoadNugetConfig(string configPath)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(configPath);

        return Settings.LoadSpecificSettings(
            expandedPath,
            Path.Combine(expandedPath, "NuGet.Config"));
    }

    private static bool ShouldRequestNugetData(string configPath, string feedName)
    {
        var settings = LoadNugetConfig(configPath);

        var sourceProvider = new PackageSourceProvider(settings);
        var sources = sourceProvider.LoadPackageSources().ToList();

        var existingSource = sources.FirstOrDefault(s => s.Name == feedName);

        // 2. Logic Check
        return existingSource?.Credentials == null;
    }

    private static PackageSourceCredential BuildCredentials(string name, string user, string? pwd)
    {
        return new PackageSourceCredential(name, user, pwd ?? "", isPasswordClearText: true,
            validAuthenticationTypesText: null);
    }

    public static void SetNugetCredentials(string configPath, string name, string url, string user, string? pwd)
    {
        var settings = LoadNugetConfig(configPath);

        var sourceProvider = new PackageSourceProvider(settings);
        var sources = sourceProvider.LoadPackageSources().ToList();

        var existingSource = sources.FirstOrDefault(s => s.Name == name);

        // 2. Logic Check
        if (existingSource != null)
        {
            existingSource.Credentials = BuildCredentials(name, user, pwd);
        }
        else
        {
            // 3. Source doesn't exist at all, add a new one
            var newSource = new PackageSource(url, name)
            {
                Credentials = BuildCredentials(name, user, pwd)
            };
            sources.Add(newSource);
        }

        // 4. Save the updated list back to the config
        sourceProvider.SavePackageSources(sources);
    }

    public static async Task<bool> UpdatePackagesAsync(CSharpSolution solution, ICollection<string> ignoredDeps,
        CancellationToken ct = default)
    {
        try
        {
            var solutionDir = Path.GetDirectoryName(solution.Path)!;
            foreach (var packageId in solution.ConsumesDependencies
                         .Where(x => x.IsCorporate)
                         .DistinctBy(x => x.Name)
                         .Select(x => x.Name))
            {
                // обход ограничения коллекции решения - туда записывается "основной" ИД зависимости, а не продукты (проекты)
                if (ignoredDeps.Contains(packageId) || ignoredDeps.Any(x => packageId.StartsWith($"{x}.")))
                    continue;

                await TerminalHelper.DotnetPackageUpdateAsync(solutionDir, packageId, ct);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update packages");
            return false;
        }

        return true;
    }
}