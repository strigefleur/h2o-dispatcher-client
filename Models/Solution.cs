using System.Windows.Documents;
using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record Solution
{
    public abstract SolutionKind Kind { get; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string? ChangelogVersionNumber { get; init; }
    public required SolutionType? Type { get; init; }
    public required DateTime? LatestSyncDate { get; init; }

    public abstract void Run();
}