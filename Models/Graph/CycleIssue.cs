namespace Felweed.Models.Graph;

public sealed record CycleIssue(Guid[] NodeIds) : GraphIssue("Cycle detected");