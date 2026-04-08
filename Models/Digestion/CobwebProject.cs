using NGitLab.Models;

namespace Felweed.Models.Digestion;

public sealed record CobwebProject
{
    public ProjectId Id { get; init; }
    public required string Name { get; init; }
    public required string WebUrl { get; init; }

    public List<CobwebTag> Tags { get; init; } = [];
}