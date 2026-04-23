using CliWrap;
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

        public string? GetRemoteUrl()
        {
            return repo.Network.Remotes["origin"]?.Url;
        }

        public string? GetAuthenticatedRemoteUrl(string gitlabToken)
        {
            var cleanUrl = repo.GetRemoteUrl()?.Replace("https://", "");

            return cleanUrl == null ? null : $"https://oauth2:{gitlabToken}@{cleanUrl}";
        }

        public Task<CommandResult> FetchAsync(string gitlabToken, string solutionDir, CancellationToken ct = default)
        {
            // ошибка проверки отзыва сертификата при работе через библиотеку
            // var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            // Commands.Fetch(repo, remote.Name, refSpecs, null, "");
                    
            var args = new List<string>
            {
                "-c", "http.schannelCheckRevoke=false",
                "fetch",
                repo.GetAuthenticatedRemoteUrl(gitlabToken),
                "+refs/heads/*:refs/remotes/origin/*",
                "refs/tags/*:refs/tags/*"
            };
                    
            return Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(solutionDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(ct);
        }
    }
}