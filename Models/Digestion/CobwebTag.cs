using NGitLab;

namespace Felweed.Models.Digestion;

public sealed record CobwebTag
{
    public required string Name { get; init; }
    public required string AuthorUsername { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required JobStatus? LatestPublishStatus { get; init; }
}