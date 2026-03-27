using Felweed.Models.Enumerators;
using Felweed.Services;

namespace Felweed.Models;

public record AngularSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.Angular;

    private List<AngularSolutionDependency> _dependencies = [];

    public override IReadOnlyCollection<AngularSolutionDependency> Dependencies => _dependencies
        .Where(x => x.Type == SolutionDependencyType.Corporate).ToArray().AsReadOnly();

    public void AddDependencyRange(params AngularSolutionDependency[] dependencies)
    {
        _dependencies.AddRange(dependencies);
    }

    public void ReplaceDependencies(List<AngularSolutionDependency> dependencies)
    {
        _dependencies = dependencies;
    }

    public override void Run(params string[] args)
    {
        if (Type == SolutionType.Library)
            throw new NotImplementedException();

        TerminalHelper.Run(Path, "yarn start", Name);
    }
}