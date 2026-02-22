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
}