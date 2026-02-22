using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record AngularSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.Angular;
    
    private List<AngularSolutionDependency> _dependencies = [];
    public IReadOnlyCollection<AngularSolutionDependency> Dependencies => _dependencies.AsReadOnly();
    
    public void AddDependencyRange(params AngularSolutionDependency[] dependencies)
    {
        _dependencies.AddRange(dependencies);
    }
}