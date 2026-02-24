using System.IO;
using Felweed.Models.Enumerators;
using Felweed.Services;

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
    
    public override void Run()
    {
        if (Type == SolutionType.Library)
            throw new NotImplementedException();
        
        var workingDirectory = GetWorkingDirectory();
        
        TerminalHelper.Run(workingDirectory, "dotnet run", Name);
    }

    private string GetWorkingDirectory()
    {
        var solutionDir = System.IO.Path.GetDirectoryName(Path);
        var srcPath = System.IO.Path.Combine(solutionDir, "src");
        var oldRunPath = System.IO.Path.Combine(srcPath, $"CFO.Services.{Name}");
        var newRunPath = System.IO.Path.Combine(srcPath, $"CFO.Services.{Name}.Web");

        if (Directory.Exists(oldRunPath))
        {
            return oldRunPath;
        }

        if (Directory.Exists(newRunPath))
        {
            return newRunPath;
        }

        throw new InvalidOperationException();
    }
}