using System.Collections.ObjectModel;
using System.IO;
using CliWrap;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models.Enumerators;
using Felweed.Services;
using LibGit2Sharp;
using Serilog;

namespace Felweed.ViewModels;

public partial class BatchRepoCheckoutVm : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<SolutionActualizeVm> Solutions { get; set; } = [];
    [ObservableProperty] public partial string BranchName { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ActualizeViewEnabled { get; set; } = true;
    [ObservableProperty] public partial ObservableCollection<string> AutoSuggestBoxSuggestions { get; set; } =
    [
        "feature/", "bugfix/", "rc", "master", "production", "feature/dev", "feature/catnip", "feature/ECO_H20-"
    ];
    
    public BatchRepoCheckoutVm()
    {
        foreach (var angularSolution in SolutionScanner.AngularSolutions
                     .Where(x => x is { IsCorporate: true })
                     .OrderBy(x => x.IsRunnable)
                     .ThenBy(x => x.Name))
        {
            Solutions.Add(new()
            {
                Solution = angularSolution
            });
        }
        
        foreach (var angularSolution in SolutionScanner.CsharpSolutions
                     .Where(x => x is { IsCorporate: true })
                     .OrderBy(x => x.IsRunnable)
                     .ThenBy(x => x.Name))
        {
            Solutions.Add(new()
            {
                Solution = angularSolution
            });
        }
    }

    [RelayCommand]
    private async void Process()
    {
        if (string.IsNullOrWhiteSpace(BranchName))
            return;
        
        ActualizeViewEnabled = false;
        
        foreach (var solution in Solutions)
        {
            solution.ResetStatus();
        }

        try
        {
            var gitlabToken = SecureStorage.LoadApiKey();
            
            foreach (var solutionVm in Solutions.Where(x => x.IsChecked))
            {
                try
                {
                    solutionVm.Status = SolutionActualizeStatus.InProgress;

                    var solutionDir = solutionVm.Solution.Kind == SolutionKind.Angular
                        ? solutionVm.Solution.Path
                        : Path.GetDirectoryName(solutionVm.Solution.Path);

                    using var repo = new Repository(solutionDir);
                    
                    var branch = repo.Branches[BranchName];
                    if (branch != null)
                    {
                        if (branch.IsCurrentRepositoryHead)
                        {
                            solutionVm.Status = SolutionActualizeStatus.Skipped;
                            continue;
                        }
                        
                        Commands.Checkout(repo, branch);
                        solutionVm.Status = SolutionActualizeStatus.Success;
                    }
                    else
                    { 
                        var remote = repo.Network.Remotes["origin"];
                        
                        // ошибка проверки отзыва сертификата при работе через библиотеку
                        // var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        // Commands.Fetch(repo, remote.Name, refSpecs, null, "");
                        
                        var cleanUrl = remote.Url.Replace("https://", "");
                        
                        var args = new List<string>
                        {
                            "-c", "http.schannelCheckRevoke=false",
                            "fetch",
                            $"https://oauth2:{gitlabToken}@{cleanUrl}",
                            "+refs/heads/*:refs/remotes/origin/*",
                            "refs/tags/*:refs/tags/*"
                        };
                        
                        var fetchResult = await Cli.Wrap("git")
                            .WithArguments(args)
                            .WithWorkingDirectory(solutionDir)
                            .WithValidation(CommandResultValidation.None)
                            .ExecuteAsync();
                        
                        if (fetchResult.ExitCode != 0)
                        {
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                            continue;
                        }
                        
                        var remoteBranch = repo.Branches[$"origin/{BranchName}"];
                        if (remoteBranch != null)
                        {
                            var localBranch = repo.CreateBranch(BranchName, remoteBranch.Tip);
                            Commands.Checkout(repo, localBranch);
                            solutionVm.Status = SolutionActualizeStatus.Success;
                        }
                        else
                        {
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception during solution checkout");
                    solutionVm.Status = SolutionActualizeStatus.Failed;
                }
            }
        }
        finally
        {
            ActualizeViewEnabled = true;
        }
    }
}