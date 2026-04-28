using System.IO;
using Felweed.Services;
using Wpf.Ui.Appearance;

namespace Felweed.Models.AppConfig;

public sealed record AppProfileConfig
{
    public required string? Name { get; init; }
    public required string? Description { get; init; }
    
    public List<string> SolutionDirectories { get; set; } = [];

    public required string? CSharpCorporateL0Prefix { get; init; }
    public required string? CSharpCorporateL1Prefix { get; init; }
    
    public required string? AngularCorporateL0Prefix { get; init; }
    public required string? AngularCorporateL1Prefix { get; init; }
    
    public required Uri? DepsGoogleTableUrl { get; init; }

    public ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;
    public string? ServerUrl { get; set; }
    
    public required string? ActiveBranch { get; init; }

    public Uri? GetHubUrl()
    {
        var serverUri = UrlHelper.GetSafeUrl(ServerUrl);
        if (serverUri is null)
            return null;
        
        return new Uri($"{serverUri}hubs/state");
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;
        
        if (string.IsNullOrWhiteSpace(Description))
            return false;
        
        if (SolutionDirectories.Count == 0)
            return false;

        if (SolutionDirectories.Any(x => !Directory.Exists(x)))
            return false;
        
        if (string.IsNullOrWhiteSpace(CSharpCorporateL0Prefix))
            return false;
        if (string.IsNullOrWhiteSpace(CSharpCorporateL1Prefix))
            return false;
        
        if (string.IsNullOrWhiteSpace(AngularCorporateL0Prefix))
            return false;
        if (string.IsNullOrWhiteSpace(AngularCorporateL1Prefix))
            return false;
        
        if (DepsGoogleTableUrl == null)
            return false;
        
        if (string.IsNullOrWhiteSpace(ActiveBranch))
            return false;

        return true;
    }
}