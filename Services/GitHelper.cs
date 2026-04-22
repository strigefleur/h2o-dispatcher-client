using System.IO;

namespace Felweed.Services;

public static class GitHelper
{
    public static DateTime? GetLastGitSyncDate(string repoPath)
    {
        // Ensure the path ends with .git, or find it
        var gitDir = Path.Combine(repoPath, ".git");
    
        // If the folder provided IS the .git folder, handle that
        if (Path.GetFileName(repoPath).Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            gitDir = repoPath;
        }

        var fetchHeadPath = Path.Combine(gitDir, "FETCH_HEAD");

        if (File.Exists(fetchHeadPath))
        {
            return File.GetLastWriteTime(fetchHeadPath);
        }

        // Fallback: If FETCH_HEAD doesn't exist, the repo might never have been synced
        // or it's a fresh init. You could check the config file as a fallback 
        // to verify it's a valid repo.
        return null; 
    }
}