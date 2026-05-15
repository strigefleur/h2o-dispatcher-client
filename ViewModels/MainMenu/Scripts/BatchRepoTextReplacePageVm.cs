using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;
using LibGit2Sharp;

namespace Felweed.ViewModels.MainMenu.Scripts;

public partial class BatchRepoTextReplacePageVm : ObservableObject
{
    [ObservableProperty]
    public partial bool BackendOnly { get; set; }

    [ObservableProperty]
    public partial bool FrontendOnly { get; set; }

    [ObservableProperty]
    public partial bool Both { get; set; } = true;

    [ObservableProperty]
    public partial bool ServiceOnly { get; set; }

    [ObservableProperty] public partial byte SearchDepth { get; set; } = 1;

    [ObservableProperty]
    public partial bool WithCommit { get; set; }

    [ObservableProperty]
    public partial string Filename { get; set; } = "";

    [ObservableProperty]
    public partial string? CommitMessage { get; set; }

    [ObservableProperty]
    public partial string LookupText { get; set; } = "";

    [ObservableProperty]
    public partial string ReplaceText { get; set; } = "";

    [ObservableProperty]
    public partial string ReplaceResult { get; set; } = "";

    private bool CanReplace()
    {
        if (!BackendOnly && !FrontendOnly && !Both)
        {
            ReplaceResult = "Не задан фильтр по типу репозитория";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Filename))
        {
            ReplaceResult = "Не задано название целевого файла";
            return false;
        }

        if (string.IsNullOrWhiteSpace(LookupText))
        {
            ReplaceResult = "Не задан искомый текст";
            return false;
        }

        if (WithCommit && string.IsNullOrWhiteSpace(CommitMessage))
        {
            ReplaceResult = "Не задано сообщение комита";
            return false;
        }

        return true;
    }

    private IReadOnlyCollection<Solution> SelectCollection()
    {
        if (BackendOnly)
        {
            return ServiceOnly
                ? SolutionScanner.CsharpSolutions.Where(x => x.IsRunnable).ToArray()
                : SolutionScanner.CsharpSolutions;
        }

        if (FrontendOnly)
        {
            return ServiceOnly
                ? SolutionScanner.AngularSolutions.Where(x => x.IsRunnable).ToArray()
                : SolutionScanner.AngularSolutions;
        }

        if (ServiceOnly)
        {
            return
            [
                ..SolutionScanner.AngularSolutions
                    .Where(x => x.IsRunnable).ToArray(),
                ..SolutionScanner.CsharpSolutions
                    .Where(x => x.IsRunnable).ToArray()
            ];
        }

        return [..SolutionScanner.AngularSolutions, ..SolutionScanner.CsharpSolutions];
    }

    private (string Dir, string Filename)? ShouldTrySolution(Solution solution)
    {
        if (solution.IsCorporate != true)
            return null;

        var dir = Directory.Exists(solution.Path) ? solution.Path : Path.GetDirectoryName(solution.Path);
        if (dir == null)
            return null;

        var filename = FindFileWithDepth(dir, Filename, SearchDepth);
        if (!File.Exists(filename))
            return null;

        if (!Repository.IsValid(dir))
            return null;

        return (dir, filename);
    }

    [RelayCommand]
    private void Replace()
    {
        ReplaceResult = string.Empty;

        if (!CanReplace())
            return;

        var replaceCount = 0;
        foreach (var solution in SelectCollection())
        {
            var checkInfo = ShouldTrySolution(solution);
            if (checkInfo == null)
                continue;

            var content = File.ReadAllText(checkInfo.Value.Filename);

            if (content.Contains(LookupText))
            {
                var updatedContent = content.Replace(LookupText, ReplaceText);
                File.WriteAllText(checkInfo.Value.Filename, updatedContent);

                ReplaceResult += $"\n{++replaceCount}: выполнена замена в {checkInfo.Value.Filename}";

                if (WithCommit)
                {
                    using (var repo = new Repository(checkInfo.Value.Dir))
                    {
                        var defaultSignature = repo.Config.BuildSignature(DateTimeOffset.Now);

                        Commands.Stage(repo, (string)Filename);
                        if (repo.RetrieveStatus().IsDirty)
                        {
                            repo.Commit(CommitMessage, defaultSignature, defaultSignature);
                        }
                    }
                }
            }
        }
    }

    [RelayCommand]
    private void ReplaceDryRun()
    {
        ReplaceResult = string.Empty;

        if (!CanReplace())
            return;

        var replaceCount = 0;
        foreach (var solution in SelectCollection())
        {
            var checkInfo = ShouldTrySolution(solution);
            if (checkInfo == null)
                continue;

            var content = File.ReadAllText(checkInfo.Value.Filename);

            if (content.Contains(LookupText))
            {
                ReplaceResult += $"\n{++replaceCount}: выполнилась бы замена в {checkInfo.Value.Filename}";
            }
        }
    }
    
    private static string? FindFileWithDepth(string currentDir, string targetName, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth || !Directory.Exists(currentDir))
            return null;

        try
        {
            // 1. Check current directory for exact match (Case-insensitive by default on Windows)
            var matchedFile = Directory.EnumerateFiles(currentDir, targetName).FirstOrDefault();
            if (matchedFile != null)
                return matchedFile;

            // 2. If not found and depth remains, check subdirectories
            if (currentDepth < maxDepth)
            {
                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    var result = FindFileWithDepth(subDir, targetName, maxDepth, currentDepth + 1);
                    if (result != null)
                        return result; // Return immediately on first match
                }
            }
        }
        catch (UnauthorizedAccessException) { /* Skip locked system folders */ }
    
        return null;
    }
}