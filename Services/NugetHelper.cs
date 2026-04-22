using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Felweed.Constants;
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

    public static async Task<bool> ResolveUpdates(string workDir, int maxParallelism = 4, CancellationToken ct = default)
    {
        try
        {
            // 1. Get outdated packages (same as before)
            var jsonOutput = await GetOutdatedPackagesJson(workDir, ct);
            if (string.IsNullOrEmpty(jsonOutput))
                return false;

            using var doc = JsonDocument.Parse(jsonOutput);
            var updateTasks = new List<Task<bool>>();
        
            // Use SemaphoreSlim to limit concurrent dotnet processes
            using var semaphore = new SemaphoreSlim(maxParallelism);

            foreach (var project in doc.RootElement.GetProperty("projects").EnumerateArray())
            {
                var projectPath = Path.GetDirectoryName(project.GetProperty("path").GetString());
                if (!project.TryGetProperty("frameworks", out var frameworks)) continue;

                foreach (var framework in frameworks.EnumerateArray())
                {
                    if (!framework.TryGetProperty("topLevelPackages", out var packages)) continue;

                    foreach (var pkg in packages.EnumerateArray())
                    {
                        var id = pkg.GetProperty("id").GetString()!;
                        var latest = pkg.GetProperty("latestVersion").GetString();

                        if (id.StartsWith(PrefixConst.CSharpCorporateL0Prefix) && !string.IsNullOrEmpty(latest))
                        {
                            // Pass the semaphore to the execution method
                            updateTasks.Add(RunThrottledUpdateAsync(projectPath!, id, latest, semaphore, ct));
                        }
                    }
                }
            }

            var results = await Task.WhenAll(updateTasks);
            return results.All(success => success);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to resolve updates");
            return false;
        }
    }

    private static async Task<bool> RunThrottledUpdateAsync(string workingDir, string id, string version,
        SemaphoreSlim semaphore, CancellationToken ct)
    {
        // Wait for an available slot
        await semaphore.WaitAsync(ct);
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"add package {id} --version {version}",
                WorkingDirectory = workingDir,
                CreateNoWindow = true
            });

            if (process == null) return false;
            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        finally
        {
            // Always release the slot, even if the process fails
            semaphore.Release();
        }
    }
    
    private static async Task<string> GetOutdatedPackagesJson(string workDir, CancellationToken ct)
    {
        using var listProcess = new Process();
        listProcess.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "list package --outdated --format json",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        listProcess.Start();
    
        // Captures the entire JSON string from the process output
        var jsonOutput = await listProcess.StandardOutput.ReadToEndAsync(ct);
        await listProcess.WaitForExitAsync(ct);

        // Return the raw JSON or null/empty if the process failed
        return listProcess.ExitCode == 0 ? jsonOutput : string.Empty;
    }

}