using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Extensions;
using Felweed.Models.Enumerators;
using Felweed.Services;
using LibGit2Sharp;
using Serilog;

namespace Felweed.ViewModels.MainMenu.Scripts;

public partial class BatchRepoCheckoutPageVm : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<SolutionActualizeVm> Solutions { get; set; } = [];
    [ObservableProperty] public partial string BranchName { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ActualizeViewEnabled { get; set; } = true;
    [ObservableProperty] public partial int ProcessorCount { get; set; } = Environment.ProcessorCount;
    [ObservableProperty] public partial int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    [ObservableProperty]
    public partial ObservableCollection<string> AutoSuggestBoxSuggestions { get; set; } =
    [
        "feature/", "bugfix/", "rc", "master", "production", "feature/dev", "feature/catnip", "feature/ECO_H20-"
    ];

    public BatchRepoCheckoutPageVm()
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
    private async Task Find(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(BranchName))
            return;

        ActualizeViewEnabled = false;

        try
        {
            var gitlabToken = SecureStorage.LoadApiKey();

            await Parallel.ForEachAsync(Solutions,
                new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                async (solutionVm, token) =>
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
                            if (!await repo.FetchAsync(gitlabToken, solutionDir, token))
                            {
                                var remoteBranch = repo.Branches[$"origin/{BranchName}"];
                                if (remoteBranch != null)
                                {
                                    solutionVm.IsChecked = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An exception during filter solutions with branch");
                    }
                });
        }
        finally
        {
            ActualizeViewEnabled = true;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        foreach (var solution in Solutions)
        {
            solution.ResetStatus();
            solution.IsChecked = false;
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
            var gitlabToken = SecureStorage.LoadApiKey();
            if (gitlabToken == null)
                return;

            await Parallel.ForEachAsync(Solutions.Where(x => x.IsChecked),
                new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                async (solutionVm, token) =>
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
                                if (!await repo.PullAsync(gitlabToken, solutionDir, BranchName, token))
                                {
                                    solutionVm.Status = SolutionActualizeStatus.Failed;
                                }
                                else
                                {
                                    solutionVm.Status = SolutionActualizeStatus.Skipped;
                                }
                            }
                            else
                            {
                                Commands.Checkout(repo, branch);
                                
                                if (!await repo.PullAsync(gitlabToken, solutionDir, BranchName, token))
                                {
                                    solutionVm.Status = SolutionActualizeStatus.Failed;
                                }
                                else
                                {
                                    solutionVm.Status = SolutionActualizeStatus.Success;
                                }
                            }
                        }
                        else
                        {
                            if (!await repo.FetchAsync(gitlabToken, solutionDir, token))
                            {
                                solutionVm.Status = SolutionActualizeStatus.Failed;
                            }
                            else
                            {
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An exception during solution checkout");
                        solutionVm.Status = SolutionActualizeStatus.Failed;
                    }
                });
        }
        finally
        {
            ActualizeViewEnabled = true;
        }
    }
}