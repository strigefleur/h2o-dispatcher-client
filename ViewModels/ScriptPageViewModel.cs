using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;
using LibGit2Sharp;

namespace Felweed.ViewModels;

public partial class ScriptPageViewModel : ObservableObject
{
    #region ReplaceText
    
    [ObservableProperty] private bool _backendOnly;
    [ObservableProperty] private bool _frontendOnly;
    [ObservableProperty] private bool _both = true;
    [ObservableProperty] private bool _serviceOnly;
    
    [ObservableProperty] private bool _withCommit;
    
    [ObservableProperty] private string _filename = "";
    [ObservableProperty] private string? _commitMessage;
    
    [ObservableProperty] private string _lookupText = "";
    [ObservableProperty] private string _replaceText = "";
    [ObservableProperty] private string _replaceResult = "";
    
    #endregion Replace
    
    #region ActualizeVersion
    
    [ObservableProperty] private ObservableCollection<Solution> _frontendServices = [];
    [ObservableProperty] private bool _skipBuild;
    [ObservableProperty] private string _actualizeResult = "";
    
    #endregion

    public ScriptPageViewModel()
    {
        foreach (var angularSolution in SolutionScanner.AngularSolutions
                     .Where(x => x is { IsCorporate: true, IsRunnable: true }))
        {
            FrontendServices.Add(angularSolution);
        }
    }

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

    [RelayCommand]
    private async Task ActualizeFrontendDeps(AngularSolution? solution)
    {
        ActualizeResult = string.Empty;

        if (solution == null)
        {
            ActualizeResult = $"{DateTime.Now}: Не выбран проект";
            return;
        }

        ActualizeResult = $"{DateTime.Now}: Начало актуализации {solution.Name}...";
        
        ActualizeResult += $"\n{DateTime.Now}: Выполнение [npm-check-updates]...";
        if (!await RunCmd("npx", @"--strict-ssl=false npm-check-updates -p yarn -f /^@rshbgroup\.cfo\// -u --install always", solution.Path))
        {
            ActualizeResult += $"\n{DateTime.Now}: Ошибка при выполнении [npm-check-updates]";
            return;
        }

        if (!SkipBuild)
        {
            ActualizeResult += $"\n{DateTime.Now}: Выполнение [yarn build]...";
            if (!await RunCmd("yarn", "build", solution.Path))
            {
                ActualizeResult += $"\n{DateTime.Now}: Ошибка при выполнении [yarn build]";
                return;
            }
        }
        else
        {
            ActualizeResult += $"\n{DateTime.Now}: Выполнение [yarn build] пропускается...";
        }
        
        ActualizeResult += $"\n{DateTime.Now}: Создание комита...";
        if (!await RunCmd("git", "add .", solution.Path))
        {
            ActualizeResult += $"\n{DateTime.Now}: Ошибка stage комита";
            return;
        }

        if (!await RunCmd("git", "commit -m \"chore: bump deps\"", solution.Path))
        {
            ActualizeResult += $"\n{DateTime.Now}: Ошибка при создании комита";
            return;
        }
        
        ActualizeResult += $"\n{DateTime.Now}: Готово!";
    }

    private static async Task<bool> RunCmd(string cmd, string args, string workDir)
    {
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd} {args}",
                WorkingDirectory = workDir,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var proc = Process.Start(info);
            if (proc == null)
                return false;

            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}