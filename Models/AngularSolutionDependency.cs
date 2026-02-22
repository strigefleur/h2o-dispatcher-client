using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record AngularSolutionDependency : SolutionDependency
{
    public AngularSolutionDependency(string name, string version, AngularDependencyType angularDependencyType) : base(name, version)
    {
        AngularDependencyType = angularDependencyType;
    }

    public AngularDependencyType AngularDependencyType { get; }
    public override string CorporateDepPrefix => "@rshbintech";
}