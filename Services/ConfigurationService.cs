using System.IO;
using System.Text.Json;
using Felweed.Models;

namespace Felweed.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private const string ConfigFileName = "appconfig.json";

    public ConfigurationService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configPath = Path.Combine(appData, "Felweed", ConfigFileName);
    }

    public AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch
        {
            // Log error if needed
        }
        return new AppConfig();
    }

    public void SaveConfig(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    public bool ValidateDirectories(AppConfig config)
    {
        return config.SolutionDirectories.Any(dir => 
            !string.IsNullOrWhiteSpace(dir) && 
            Directory.Exists(dir));
    }
}