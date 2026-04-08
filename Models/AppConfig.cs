using Felweed.Constants;
using Wpf.Ui.Controls;

namespace Felweed.Models;

public sealed record AppConfig
{
    public List<string> SolutionDirectories { get; set; } = [];

    public List<string> CSharpSolutionPrefixes { get; set; } =
        [Constants.PrefixConst.CSharpCorporateL2Prefix, "DataHub"];

    public SymbolRegular ThemeSwitchIcon { get; set; } = SymbolRegular.WeatherMoon24;
    public string? ServerUrl { get; set; }

    public List<EnvVariable> EnvVariables { get; set; } = [..EnvVariableConst.DefaultEnvVariables];
    
    public Uri? GetServerUrl()
    {
        if (ServerUrl is null)
            return null;

        if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return null;
        
        return uriResult;
    }

    public Uri? GetHubUrl()
    {
        var serverUri = GetServerUrl();
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