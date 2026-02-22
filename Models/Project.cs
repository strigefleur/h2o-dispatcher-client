using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record Project
{
    public abstract ProjectKind Kind { get; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string? ChangelogVersionNumber { get; init; }
    public required ProjectType? Type { get; init; }
    public required DateTime? LatestSyncDate { get; init; }
}