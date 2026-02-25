using Felweed.Models.Enumerators;
using Felweed.Services;

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
    
    public override void Run()
    {
        if (Type == SolutionType.Library)
            throw new NotImplementedException();
        
        TerminalHelper.Run(Path, "yarn start", Name);
    }
}