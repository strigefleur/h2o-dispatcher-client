using System.IO;
using System.Text.Json;
using Ardalis.GuardClauses;
using Felweed.Extensions;
using Felweed.Models;
using Felweed.Models.AppConfig;
using Serilog;
using Wpf.Ui.Controls;

namespace Felweed.Services;

public static class ConfigurationService
{
    private const string ConfigFileName = "appconfig.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

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
            catch (Exception ex)
            {
                _appConfig = new AppConfig();
                Log.Error(ex, "Failed to load appconfig");
            }
        }
        
        return _appConfig;
    }

    public static void SaveConfig()
    {
        Guard.Against.Null(_appConfig);
        
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var json = JsonSerializer.Serialize(_appConfig, Options);
        File.WriteAllText(ConfigPath, json);
    }
    
    public static void SetThemeBySymbol(SymbolRegular value)
    {
        if (_appConfig?.CurrentProfile != null)
        {
            _appConfig.ActiveProfile.Theme = value.GetTheme();
            SaveConfig();
        }
    }
    
    public static void SetActiveProfile(string? profileName)
    {
        if (_appConfig == null)
            return;
        
        if (profileName != null && !_appConfig.Profiles.ContainsKey(profileName))
            return;
        
        _appConfig.CurrentProfileName = profileName;
        
        SaveConfig();
    }
    
    public static NugetFeedConfig ReadNugetFeedConfig()
    {
        return new()
        {
            ConfigPath = _appConfig.NugetConfigPath,
            Name = _appConfig.CorporateNexusSourceName,
            Url = _appConfig.CorporateNexusSourceUrl,
        };
    }

    public static bool IsCredentialsValid()
    {
        if (_appConfig?.CorporateNexusSourceName == null)
            return false;
        
        if (_appConfig.CorporateNexusSourceUrl == null)
            return false;

        if (SecureStorage.LoadApiKey() == null)
            return false;

        return true;
    }

    public static void UpdateProfile(AppProfileConfig config)
    {
        var profile = _appConfig?.Profiles[config.Name];
        if (profile == null)
            return;

        _appConfig?.Profiles[config.Name] = config;
        SaveConfig();
    }
}