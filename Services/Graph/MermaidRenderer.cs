using Felweed.Models;

namespace Felweed.Services.Graph;

public static class MermaidRenderer
{
    public static string RenderByTiers(IReadOnlyList<IReadOnlyList<Solution>> tiers, IReadOnlyCollection<CorporateEdge> edges)
    {
        var all = tiers.SelectMany(t => t).ToList();
        var idByPath = all
            .Select((s, i) => (s.Path, Id: $"N{i}"))
            .ToDictionary(x => x.Path, x => x.Id, StringComparer.OrdinalIgnoreCase);

        static string Esc(string s) => s.Replace("\\", "/").Replace("\"", "'");

        var lines = new List<string>
        {
            "```mermaid",
            "flowchart TB"
        };

        for (int i = 0; i < tiers.Count; i++)
        {
            lines.Add($"  subgraph Tier{i}[\"Tier {i}\"]");
            foreach (var s in tiers[i])
                lines.Add($"    {idByPath[s.Path]}[\"{Esc(s.Name)}\"]");
            lines.Add("  end");
        }

        foreach (var e in edges)
        {
            if (!idByPath.TryGetValue(e.From.Path, out var fromId)) continue;
            if (!idByPath.TryGetValue(e.To.Path, out var toId)) continue;

            // label edge with package name (optional)
            lines.Add($"  {fromId} -->|\"{Esc(e.PackageName)}\"| {toId}");
        }

        lines.Add("```");
        return string.Join(Environment.NewLine, lines);
    }
}