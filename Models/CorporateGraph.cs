namespace Felweed.Models;

public sealed record CorporateGraph
{
    public required IReadOnlyCollection<Solution> Nodes { get; init; }
    public required IReadOnlyCollection<CorporateEdge> Edges { get; init; }
}