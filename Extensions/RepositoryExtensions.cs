using LibGit2Sharp;
using NuGet.Versioning;

namespace Felweed.Extensions;

public static class RepositoryExtensions
{
    extension(Repository repo)
    {
        public string? GetLatestTagVersion()
        {
            // 1. Get all local tags
            // 2. Parse names into Semantic Versions
            // 3. Filter out any that don't follow versioning (like 'alpha-test')
            // 4. Grab the highest version
            var latestTag = repo.Tags
                .Select(t => new { 
                    Tag = t, 
                    Version = NuGetVersion.TryParse(t.FriendlyName, out var v) ? v : null 
                })
                .Where(x => x.Version != null)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
        
            return latestTag?.Version?.ToString();
        }

        public string? GetRemote()
        {
            return repo.Network.Remotes["origin"]?.Url;
        }
    }
}