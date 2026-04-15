using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;
using LibGit2Sharp;

namespace Felweed.ViewModels;

public partial class ScriptPageViewModel : ObservableObject
{
    [ObservableProperty] private bool _backendOnly ;
    [ObservableProperty] private bool _frontendOnly;
    [ObservableProperty] private bool _both = true;
    
    [ObservableProperty] private bool _withCommit;
    
    [ObservableProperty] private string _filename = "";
    [ObservableProperty] private string? _commitMessage;
    
    [ObservableProperty] private string _lookupText = "";
    [ObservableProperty] private string _replaceText = "";
    [ObservableProperty] private string _replaceResult = "";

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
            return SolutionScanner.CsharpSolutions;
        }

        if (FrontendOnly)
        {
            return SolutionScanner.AngularSolutions;
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
            
        var filename = Path.Combine(dir, Filename);
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
                        
                        Commands.Stage(repo, Filename);
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
}