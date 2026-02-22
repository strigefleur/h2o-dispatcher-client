using System.IO;
using System.IO.Enumeration;

namespace Felweed.Services;

public static class FileHelper
{
    public static IEnumerable<string> EnumerateFilesWithExclusions(this string rootPath, string searchPattern = "*")
    {
        return new FileSystemEnumerable<string>(
            rootPath,
            (ref FileSystemEntry entry) => entry.ToFullPath(),
            ScanOptions)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory &&
                                                                    FileSystemName.MatchesWin32Expression(searchPattern,
                                                                        entry.FileName),
            // Don't recurse into excluded directories
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                foreach (var excluded in DirectoriesToSkip)
                {
                    if (entry.FileName.Equals(excluded.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }
        };
    }

    private static readonly HashSet<string> DirectoriesToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        "bin",
        "obj",
        ".git",
        ".vs",
        "packages"
    };

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // Skips symbolic links to avoid loops
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden
    };
}