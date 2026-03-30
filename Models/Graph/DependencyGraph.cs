namespace Felweed.Models.Graph;

public sealed class DependencyGraph
{
    public required IReadOnlyDictionary<Guid, Node> Nodes { get; init; }
    public required IReadOnlyList<Edge> Edges { get; init; }

    // Индексы для быстрых запросов
    public required IReadOnlyDictionary<Guid, IReadOnlyList<Edge>> Outgoing { get; init; } // producer -> consumers
    public required IReadOnlyDictionary<Guid, IReadOnlyList<Edge>> Incoming { get; init; } // consumer <- producers

    public required IReadOnlyList<GraphIssue> Issues { get; init; } // missing/ambiguous/cycle etc.
}