using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record CSharpSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.CSharp;

    private List<CSharpSolutionDependency> _dependencies = [];
    public IReadOnlyCollection<CSharpSolutionDependency> Dependencies => _dependencies.AsReadOnly();

    public void AddDependencyRange(params CSharpSolutionDependency[] dependencies)
    {
        _dependencies.AddRange(dependencies);
    }

    public void ReplaceDependencies(List<CSharpSolutionDependency> dependencies)
    {
        _dependencies = dependencies;
    }
}