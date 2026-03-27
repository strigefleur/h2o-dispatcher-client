using System.IO;
using System.Text.Json;
using Ardalis.GuardClauses;
using Felweed.Models;

namespace Felweed.Services;

public static class ConfigurationService
{
    private const string ConfigFileName = "appconfig.json";

    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Felweed", ConfigFileName);
    
    private static AppConfig? _appConfig;

    public static AppConfig LoadConfig()
    {
        if (_appConfig == null)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _appConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    _appConfig = new AppConfig();
                }
            }
            catch
            {
                // Log error if needed
                _appConfig = new AppConfig();
            }
        }
        
        return _appConfig;
    }

    public static void SaveConfig()
    {
        Guard.Against.Null(_appConfig);
        
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var json = JsonSerializer.Serialize(_appConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public static bool ValidateDirectories()
    {
        Guard.Against.Null(_appConfig);
        
        return _appConfig.SolutionDirectories.Any(dir => 
            !string.IsNullOrWhiteSpace(dir) && 
            Directory.Exists(dir));
    }
}