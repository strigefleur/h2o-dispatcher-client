using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Extensions;
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
    [ObservableProperty] public partial bool AutoSearch { get; set; } = true;
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
    
    private async Task FilterSolutionsWithBranch(CancellationToken ct = default)
    {
        var gitlabToken = SecureStorage.LoadApiKey();
        
        foreach (var solutionVm in Solutions)
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
                    solutionVm.IsChecked = true;
                }
                else
                {
                    var fetchResult = await repo.FetchAsync(gitlabToken, solutionDir, ct);
                    if (fetchResult.ExitCode != 0)
                    {
                        continue;
                    }
                    
                    var remoteBranch = repo.Branches[$"origin/{BranchName}"];
                    if (remoteBranch != null)
                    {
                        solutionVm.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception during filter solutions with branch");
            }
        }
    }

    [RelayCommand]
    private async Task Process(CancellationToken ct = default)
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
            if (AutoSearch)
            {
                await FilterSolutionsWithBranch(ct);
            }
            
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
                        var fetchResult = await repo.FetchAsync(gitlabToken, solutionDir, ct);
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