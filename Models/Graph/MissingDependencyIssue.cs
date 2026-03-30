namespace Felweed.Models.Graph;

public sealed record MissingDependencyIssue(Guid ConsumerId, string DependencyName) : GraphIssue($"Missing: {DependencyName}");