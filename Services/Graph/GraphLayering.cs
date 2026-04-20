using Felweed.Models.Graph;

namespace Felweed.Services.Graph;

public static class GraphLayering
{
    public static IReadOnlyList<IReadOnlyList<Guid>> BuildLayers(DependencyGraph g)
    {
        var indegree = g.Nodes.Keys.ToDictionary(id => id, _ => 0);

        foreach (var e in g.Edges)
            indegree[e.ToId]++;

        var queue = new Queue<Guid>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var layers = new List<List<Guid>>();

        while (queue.Count > 0)
        {
            var layer = new List<Guid>();
            var layerCount = queue.Count;

            for (int i = 0; i < layerCount; i++)
            {
                var n = queue.Dequeue();
                layer.Add(n);

                if (!g.Outgoing.TryGetValue(n, out var outEdges))
                    continue;

                foreach (var e in outEdges)
                {
                    indegree[e.ToId]--;
                    if (indegree[e.ToId] == 0)
                        queue.Enqueue(e.ToId);
                }
            }

            layers.Add(layer);
        }

        // если остались indegree>0 => цикл. По-хорошему: SCC, но минимум — сигнализировать.
        var cyclic = indegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToArray();
        if (cyclic.Length > 0)
        {
            // при желании добавить Issue
            // issues.Add(new CycleIssue(cyclic));
        }

        return layers;
    }
    
    public static List<Guid> TopoSortLocal(DependencyGraph g, HashSet<Guid> subset)
    {
        var visited = new HashSet<Guid>();
        var result = new List<Guid>();

        void Visit(Guid id)
        {
            if (!subset.Contains(id) || !visited.Add(id)) return;
            if (g.Outgoing.TryGetValue(id, out var edges))
                foreach (var e in edges)
                    Visit(e.ToId);
            result.Add(id);
        }

        foreach (var id in subset) Visit(id);
        result.Reverse();
        return result;
    }
}