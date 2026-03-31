namespace Felweed.Models;

public record ConsumedDependency(string Name, string Version)
{
    public Solution? Solution { get; set; }

    public bool IsCorporate =>
        Name.StartsWith(Constants.PrefixConst.AngularCorporateDepPrefix, StringComparison.OrdinalIgnoreCase) ||
        Name.StartsWith(Constants.PrefixConst.CSharpCorporateL0Prefix, StringComparison.OrdinalIgnoreCase);
}