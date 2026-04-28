using Felweed.Models;
using Felweed.Models.Graph;

namespace Felweed.Services.Graph;

public static class DependencyGraphBuilder
{
    public static DependencyGraph Build(IReadOnlyCollection<Solution> solutions)
    {
        var nodes = solutions.ToDictionary(s => s.Id, s => new Node { Solution = s });

        // 1) Индекс: packageName -> producerSolutionId
        // (условия: форков нет => один producer)
        var producedBy = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in solutions)
        {
            foreach (var depName in s.ProducesDependencies)
            {
                // Если вдруг окажется дубль — это уже проблема данных
                if (!producedBy.TryAdd(depName, s.Id))
                {
                    // можно бросать исключение или Issue
                    // issues.Add(new AmbiguousProducerIssue(...))
                }
            }
        }

        var edges = new List<Edge>();
        var issues = new List<GraphIssue>();

        foreach (var consumer in solutions)
        {
            foreach (var cd in consumer.ConsumesDependencies)
            {
                if (!cd.IsCorporate()) continue;

                // ВАЖНО: внешнее/отсутствующее не включаем в граф уровней
                if (!producedBy.TryGetValue(cd.Name, out var producerId)) continue;

                // Самозависимость можно отфильтровать (на всякий случай)
                if (producerId == consumer.Id) continue;

                edges.Add(new Edge(
                    FromId: producerId,
                    ToId: consumer.Id,
                    DependencyName: cd.Name,
                    RequestedVersion: cd.Version
                ));
            }
        }

        var outgoing = edges
            .GroupBy(e => e.FromId)
            .ToDictionary(g => g.Key, IReadOnlyList<Edge> (g) => g.ToList());

        var incoming = edges
            .GroupBy(e => e.ToId)
            .ToDictionary(g => g.Key, IReadOnlyList<Edge> (g) => g.ToList());

        return new DependencyGraph
        {
            Nodes = nodes,
            Edges = edges,
            Outgoing = outgoing,
            Incoming = incoming,
            Issues = issues
        };
    }
}