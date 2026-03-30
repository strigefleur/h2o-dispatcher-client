using Felweed.Models.Graph;

namespace Felweed.Services.Graph;

public static class TreeProjection
{
    public static DependencyTreeNode BuildDependencyTree(DependencyGraph g, Guid rootId)
        => Build(g, rootId, new HashSet<Guid>());

    private static DependencyTreeNode Build(DependencyGraph g, Guid id, HashSet<Guid> path)
    {
        var s = g.Nodes[id].Solution;

        // cycle guard for UI
        if (!path.Add(id))
        {
            return new DependencyTreeNode
            {
                SolutionId = id,
                Title = $"{s.Name} (cycle)",
                Children = Array.Empty<DependencyTreeNode>()
            };
        }

        var deps = g.Incoming.TryGetValue(id, out var incoming) ? incoming : Array.Empty<Edge>();

        // children = producers
        var children = deps
            .Select(e => e.FromId)
            .Distinct()
            .Select(childId => Build(g, childId, path))
            .ToList();

        path.Remove(id);

        return new DependencyTreeNode
        {
            SolutionId = id,
            Title = s.Name,
            Children = children
        };
    }
}