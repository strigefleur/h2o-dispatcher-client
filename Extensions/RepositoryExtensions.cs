using CliWrap;
using CliWrap.Buffered;
using LibGit2Sharp;
using NuGet.Versioning;
using Serilog;

namespace Felweed.Extensions;

public static class RepositoryExtensions
{
    extension(Repository repo)
    {
        public string? GetLatestTagVersion()
        {
            var latestTag = repo.Tags
                .Select(t => new
                {
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

        private string? GetAuthenticatedRemoteUrl(string gitlabToken)
        {
            var cleanUrl = repo.GetRemoteUrl()?.Replace("https://", "");

            return cleanUrl == null ? null : $"https://oauth2:{gitlabToken}@{cleanUrl}";
        }

        public async Task<bool> FetchAsync(string gitlabToken, string solutionDir, CancellationToken ct = default)
        {
            // ошибка проверки отзыва сертификата при работе через библиотеку
            // var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            // Commands.Fetch(repo, remote.Name, refSpecs, null, "");

            var authUrl = repo.GetAuthenticatedRemoteUrl(gitlabToken);
            if (authUrl is null)
                return false;

            var args = new List<string>
            {
                "-c", "http.schannelCheckRevoke=false",
                "fetch",
                authUrl,
                "+refs/heads/*:refs/remotes/origin/*",
                "refs/tags/*:refs/tags/*"
            };

            var result = await Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(solutionDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(ct);

            if (!result.IsSuccess)
            {
                Log.Error(result.StandardError);
                return false;
            }

            return true;
        }

        public async Task<bool> PullAsync(string gitlabToken, string solutionDir, string branchName,
            CancellationToken ct = default)
        {
            var authUrl = repo.GetAuthenticatedRemoteUrl(gitlabToken);
            if (authUrl is null)
                return false;
            
            var args = new List<string>
            {
                "-c", "http.schannelCheckRevoke=false",
                "pull",
                authUrl,
                branchName,
                "--ff-only",
                "--tags"
            };

            var result = await Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(solutionDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(ct);

            if (!result.IsSuccess)
            {
                Log.Error(result.StandardError);
                return false;
            }

            return true;
        }

        public async Task<bool> StageAndCommitAsync(string solutionDir, string commitMessage,
            CancellationToken ct = default)
        {
            var stageResult = await Cli.Wrap("git")
                .WithArguments("add .")
                .WithWorkingDirectory(solutionDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(ct);

            if (!stageResult.IsSuccess)
            {
                Log.Error(stageResult.StandardError);
                return false;
            }

            var commitResult = await Cli.Wrap("git")
                .WithArguments($"commit -m \"{commitMessage}\"")
                .WithWorkingDirectory(solutionDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(ct);

            if (!commitResult.IsSuccess)
            {
                Log.Error(commitResult.StandardError);
                return false;
            }

            return true;
        }
    }
}