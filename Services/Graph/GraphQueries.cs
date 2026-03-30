using Felweed.Models.Graph;

namespace Felweed.Services.Graph;

public static class GraphQueries
{
    // Returns: start + all downstream consumers (direct and transitive)
    public static HashSet<Guid> GetDownstreamInclusive(DependencyGraph g, Guid startId)
    {
        var visited = new HashSet<Guid> { startId };
        var q = new Queue<Guid>();
        q.Enqueue(startId);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            if (!g.Outgoing.TryGetValue(cur, out var outEdges))
                continue;

            foreach (var e in outEdges)
            {
                var next = e.ToId; // consumer
                if (visited.Add(next))
                    q.Enqueue(next);
            }
        }

        return visited;
    }
}