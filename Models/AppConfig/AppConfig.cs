using System.IO;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Felweed.Constants;

namespace Felweed.Models.AppConfig;

public sealed record AppConfig
{
    public string? CurrentProfileName { get; set; }
    
    public string? NugetConfigPath { get; set; } = "%appdata%/nuget";
    public string? CorporateNexusSourceUrl { get; set; }
    public string? CorporateNexusSourceName { get; set; }
    public string? EncryptedGitlabKey { get; set; }
    
    public List<EnvVariable> EnvVariables { get; init; } = [..EnvVariableConst.DefaultEnvVariables];
    
    public string? AnecdoteUrl { get; init; } = "https://shortiki.com/export/api.php?format=json&type=random&amount=1";

    public Dictionary<string, AppProfileConfig> Profiles { get; init; } = new()
    {
        {
            "H2.0",
            new()
            {
                Name = "H2.0",
                Description = "Надзорный календарь (НК)",
                CSharpCorporateL0Prefix = "RSHBGroup",
                CSharpCorporateL1Prefix = "CFO",
                AngularCorporateL0Prefix = "@rshbgroup",
                AngularCorporateL1Prefix = "cfo",
                ActiveBranch = "feature/dev",
                DepsGoogleTableUrl = new Uri("https://docs.google.com/spreadsheets/d/1qGU-xha3lHp07vbx6qH-VSikyNL4yQ8LLCzqpGbWOtw")
            }
        },
        {
            "РНК",
            new()
            {
                Name = "РНК",
                Description = "Розничный НК",
                CSharpCorporateL0Prefix = "RSHBGroup",
                CSharpCorporateL1Prefix = "RNK",
                AngularCorporateL0Prefix = "@rshbgroup",
                AngularCorporateL1Prefix = "rnk",
                ActiveBranch = "feature/dev",
                DepsGoogleTableUrl = new Uri("https://docs.google.com/spreadsheets/d/1hJnYFgCeYbgHm0D82HwgXy8hnajAdEfzxo9F9hf5r2o")
            }
        }
    };

    [JsonIgnore]
    public AppProfileConfig? CurrentProfile => CurrentProfileName == null
        ? null
        : Profiles.GetValueOrDefault(CurrentProfileName);

    [JsonIgnore] public AppProfileConfig ActiveProfile => Guard.Against.Null(CurrentProfile);

    [JsonIgnore]
    public string? ExpandedNugetConfigPath => NugetConfigPath == null
        ? null
        : Path.Combine(Environment.ExpandEnvironmentVariables(NugetConfigPath), NameConstants.NugetConfigFileName);
    
    public object? this[string propertyName]
    {
        set
        {
            var profile = CurrentProfile;
            if (profile == null)
                return;
            
            switch (propertyName)
            {
                case nameof(AppProfileConfig.ServerUrl):
                    profile.ServerUrl = (string?)value;
                    break;
                case nameof(NugetConfigPath):
                    NugetConfigPath = (string?)value;
                    break;
                case nameof(CorporateNexusSourceUrl):
                    CorporateNexusSourceUrl = (string?)value;
                    break;
                case nameof(CorporateNexusSourceName):
                    CorporateNexusSourceName = (string?)value;
                    break;
                default: throw new ArgumentException("Property not found");
            }
        }
    }

    public bool Validate()
    {
        if (CurrentProfileName == null)
            return false;

        if (NugetConfigPath != null && !File.Exists(ExpandedNugetConfigPath))
            return false;

        if (Profiles.Count == 0)
            return false;

        if (CurrentProfile == null)
            return false;

        if (Profiles.Any(x => !x.Value.Validate()))
            return false;

        return true;
    }
}