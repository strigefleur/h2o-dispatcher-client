namespace Felweed.Models.Graph;

public sealed class DependencyTreeNode
{
    public required Guid SolutionId { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<DependencyTreeNode> Children { get; init; }
}