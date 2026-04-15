using Felweed.Constants;
using Felweed.Services;
using Wpf.Ui.Controls;

namespace Felweed.Models;

public sealed record AppConfig
{
    public List<string> SolutionDirectories { get; set; } = [];

    public List<string> CSharpSolutionPrefixes { get; set; } =
        [Constants.PrefixConst.CSharpCorporateL1Prefix, "DataHub"];

    public SymbolRegular ThemeSwitchIcon { get; set; } = SymbolRegular.WeatherMoon24;
    public string? ServerUrl { get; set; }
    public string AnecdoteUrl { get; set; } = "https://shortiki.com/export/api.php?format=json&type=random&amount=1";

    public List<EnvVariable> EnvVariables { get; set; } = [..EnvVariableConst.DefaultEnvVariables];

    public Uri? GetHubUrl()
    {
        var serverUri = UrlHelper.GetSafeUrl(ServerUrl);
        if (serverUri is null)
            return null;
        
        return new Uri($"{serverUri}hubs/state");
    }
    
    public object? this[string propertyName]
    {
        set
        {
            switch (propertyName)
            {
                case nameof(ServerUrl): ServerUrl = (string?)value; break;
                default: throw new ArgumentException("Property not found");
            }
        }
    }
}