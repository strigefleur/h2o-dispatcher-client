using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record CSharpSolutionDependency : ProjectDependency
{
    public CSharpSolutionDependency(string name, string version) : base(name, version)
    {
    }

    public override string CorporateDepPrefix => "RSHBIntech";
}