using System.IO;
using NuGet.Configuration;

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
}