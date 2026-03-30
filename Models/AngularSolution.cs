using Felweed.Models.Enumerators;
using Felweed.Services;

namespace Felweed.Models;

public record AngularSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.Angular;

    public override void Run(params string[] args)
    {
        if (Type == SolutionType.Library)
            throw new NotImplementedException();

        TerminalHelper.Run(Path, "yarn start", Name);
    }
}