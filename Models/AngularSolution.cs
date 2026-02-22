using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record AngularSolution : Project
{
    public override ProjectKind Kind => ProjectKind.Angular;
    
    private List<AngularSolutionDependency> _dependencies = [];
    public IReadOnlyCollection<AngularSolutionDependency> Dependencies => _dependencies.AsReadOnly();
    
    public void AddDependencyRange(params AngularSolutionDependency[] dependencies)
    {
        _dependencies.AddRange(dependencies);
    }
}