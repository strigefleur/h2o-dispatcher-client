using Felweed.Models.Enumerators;
using Felweed.Services;

namespace Felweed.Models;

public record AngularSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.Angular;

    public override void Run(params string[] args)
    {
        if (Type == SolutionType.Library)
            throw new InvalidOperationException();

        TerminalHelper.Run(Path, "yarn start", Name);
    }

    public override void Pack()
    {
        if (Type == SolutionType.Service)
            throw new InvalidOperationException();
        
        TerminalHelper.Run(Path, "yarn publish:local", $"{Name}: pack");
    }

    public override void InvalidateCache()
    {
        if (Type == SolutionType.Service)
            throw new InvalidOperationException();
        
        // TODO - анализировать все фронтовые проекты на наличие подмены?
    }
}