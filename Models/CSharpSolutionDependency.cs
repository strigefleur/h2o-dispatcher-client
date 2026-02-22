namespace Felweed.Models;

public record CSharpSolutionDependency : SolutionDependency
{
    public CSharpSolutionDependency(string name, string version) : base(name, version)
    {
    }

    public override string CorporateDepPrefix => "RSHBIntech";
}