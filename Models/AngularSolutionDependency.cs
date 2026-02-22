using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record AngularSolutionDependency : ProjectDependency
{
    public AngularSolutionDependency(string name, string version, AngularDependencyType angularDependencyType) : base(name, version)
    {
        AngularDependencyType = angularDependencyType;
    }

    public AngularDependencyType AngularDependencyType { get; }
    public override string CorporateDepPrefix => "@rshbintech";
}