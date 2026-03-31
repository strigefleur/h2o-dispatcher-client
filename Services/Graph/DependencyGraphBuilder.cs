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

        // 2) Внутренние корпоративные пакеты = те, что:
        //   - корпоративные по имени
        //   - и реально "производятся" в текущем каталоге
        bool IsInternalCorporate(string depName)
            => depName is not null
               && (depName.StartsWith(Constants.PrefixConst.AngularCorporateDepPrefix, StringComparison.OrdinalIgnoreCase)
                   || depName.StartsWith(Constants.PrefixConst.CSharpCorporateL0Prefix, StringComparison.OrdinalIgnoreCase))
               && producedBy.ContainsKey(depName);

        var edges = new List<Edge>();
        var issues = new List<GraphIssue>();

        foreach (var consumer in solutions)
        {
            foreach (var cd in consumer.ConsumesDependencies)
            {
                if (!cd.IsCorporate) continue;
                if (!IsInternalCorporate(cd.Name)) continue; // ВАЖНО: внешнее/отсутствующее не включаем в граф уровней

                var producerId = producedBy[cd.Name];

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
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Edge>)g.ToList());

        var incoming = edges
            .GroupBy(e => e.ToId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Edge>)g.ToList());

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