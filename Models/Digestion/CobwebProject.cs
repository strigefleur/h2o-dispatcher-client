namespace Felweed.Models.Digestion;

public sealed record CobwebProject
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string WebUrl { get; init; }
    public required string HttpUrl { get; init; }

    public List<CobwebTag> Tags { get; init; } = [];
}