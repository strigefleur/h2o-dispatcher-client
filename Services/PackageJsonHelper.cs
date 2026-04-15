using System.IO;
using System.Text.Json.Nodes;

namespace Felweed.Services;

public static class PackageJsonHelper
{
    public static Dictionary<string, string> ReadVersions(JsonObject pkg, params string[] sections)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var section in sections)
        {
            if (pkg[section] is JsonObject deps)
            {
                foreach (var kv in deps)
                {
                    if (kv.Value is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                        map[kv.Key] = s;
                }
            }
        }

        return map;
    }

    public static int SyncSection(JsonObject innerPkg, string sectionName, Dictionary<string, string> rootVersions)
    {
        if (innerPkg[sectionName] is not JsonObject innerDeps)
            return 0;

        int changed = 0;

        foreach (var depName in innerDeps.Select(k => k.Key).ToList())
        {
            if (!rootVersions.TryGetValue(depName, out var rootVersion))
                continue;

            var current = innerDeps[depName]?.GetValue<string>();

            if (!string.Equals(current, rootVersion, StringComparison.Ordinal))
            {
                innerDeps[depName] = rootVersion;
                changed++;
            }
        }

        return changed;
    }

    public static JsonObject LoadPackageJson(string path)
    {
        var text = File.ReadAllText(path);
        var node = JsonNode.Parse(text) ?? throw new Exception($"Failed to parse {path}");
        return node.AsObject();
    }
}