using Felweed.Models.Enumerators;

namespace Felweed.Models;

public record CSharpSolution : Project
{
    public override ProjectKind Kind => ProjectKind.CSharp;

    private List<CSharpSolutionDependency> _dependencies = [];
    public IReadOnlyCollection<CSharpSolutionDependency> Dependencies => _dependencies.AsReadOnly();

    public void AddDependencyRange(params CSharpSolutionDependency[] dependencies)
    {
        _dependencies.AddRange(dependencies);
    }
}