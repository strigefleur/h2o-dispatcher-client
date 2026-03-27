using Felweed.Models;

namespace Felweed.Services.Graph;

public static class CorporateTiering
{
    public static List<List<Solution>> ComputeLeafFirstTiers(CorporateGraph g)
    {
        var nodes = g.Nodes.ToList();

        var outDegree = nodes.ToDictionary(
            s => s.Path,
            s => g.Edges.Count(e => e.From.Path.Equals(s.Path, StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        var incomingByTo = g.Edges
            .GroupBy(e => e.To.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        var remaining = new HashSet<string>(nodes.Select(n => n.Path), StringComparer.OrdinalIgnoreCase);
        var tiers = new List<List<Solution>>();

        while (remaining.Count > 0)
        {
            var tierPaths = remaining.Where(p => outDegree[p] == 0).OrderBy(p => p).ToList();
            if (tierPaths.Count == 0)
            {
                // cycle: emit remaining as last tier (or handle SCCs if you want)
                tiers.Add(nodes.Where(n => remaining.Contains(n.Path)).OrderBy(n => n.Name).ToList());
                break;
            }

            var tier = tierPaths
                .Select(p => nodes.First(n => n.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            tiers.Add(tier);

            foreach (var p in tierPaths)
            {
                remaining.Remove(p);

                if (incomingByTo.TryGetValue(p, out var incoming))
                {
                    foreach (var edge in incoming)
                    {
                        if (remaining.Contains(edge.From.Path))
                            outDegree[edge.From.Path]--;
                    }
                }
            }
        }

        return tiers;
    }
}