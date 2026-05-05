using System.IO;
using System.Text.RegularExpressions;

namespace Felweed.Services;

public static class ChangelogHelper
{
    private static readonly Regex
        VersionRegex = new(@"^##\s+Версия\s+(?<version>\d+\.\d+\.\d+)", RegexOptions.Compiled);

    public static async Task<string?> GetLatestVersionNumberAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return null;

        // Читаем файл построчно, чтобы не загружать весь текст в память сразу
        await foreach (var line in File.ReadLinesAsync(filePath, ct))
        {
            var match = VersionRegex.Match(line);

            if (match.Success)
            {
                return match.Groups["version"].Value;
            }
        }

        return null;
    }
    
    public static void AddVersion(string filePath, string version, List<string> changes)
    {
        // 1. Read all lines from the file
        var lines = File.ReadAllLines(filePath).ToList();

        // 2. Prepare the new block
        var newVersionBlock = new List<string>
        {
            "", // Spacing before the header
            $"## Версия {version}",
            "",
            $"### Изменения версии {version}",
            ""
        };
        
        // Add each change as a bullet point
        newVersionBlock.AddRange(changes.Select(c => $"- {c}"));

        // 3. Find the injection point
        // Usually, we insert after the first line (the "# История изменений" header)
        const int insertIndex = 1; 

        // 4. Insert and save
        lines.InsertRange(insertIndex, newVersionBlock);
        File.WriteAllLines(filePath, lines);
    }
}