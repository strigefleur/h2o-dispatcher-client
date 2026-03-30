namespace Felweed.Models.Graph;

public sealed record AmbiguousProducerIssue(Guid ConsumerId, string DependencyName, Guid[] Producers) : GraphIssue($"Ambiguous: {DependencyName}");