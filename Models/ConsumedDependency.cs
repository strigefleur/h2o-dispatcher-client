using Felweed.Services;

namespace Felweed.Models;

public record ConsumedDependency(string Name, string Version)
{
    public Solution? Solution { get; set; }

    public bool IsCorporate()
    {
        var config = ConfigurationService.LoadConfig();

        return Name.StartsWith(config.ActiveProfile.AngularCorporateL0Prefix, StringComparison.OrdinalIgnoreCase) ||
               Name.StartsWith(config.ActiveProfile.CSharpCorporateL0Prefix, StringComparison.OrdinalIgnoreCase);
    }
}