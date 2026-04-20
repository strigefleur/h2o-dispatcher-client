using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Constants;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;
using LibGit2Sharp;

namespace Felweed.ViewModels;

public partial class BackendDepActualizerViewModel : ObservableObject
{
    private const string ToolName = "dotnet-outdated-tool";
    private const string Command = "dotnet";
    private const string CommandArgs = $"dotnet outdated -u -inc {PrefixConst.CSharpCorporateL0Prefix}";

    [ObservableProperty] private bool? _isInitialized;
    [ObservableProperty] private bool? _isProcessing;
    
    [ObservableProperty] private ObservableCollection<SolutionActualizeVm> _actualizeSolutions = [];
    [ObservableProperty] private bool _skipBuild;
    [ObservableProperty] private string _actualizeResult = "";
    [ObservableProperty] private bool _actualizeViewEnabled = true;
    [ObservableProperty] private bool _canInterruptActualization;
    [ObservableProperty] private SolutionActualizeVm? _dagFilterSolution;

    private CancellationTokenSource? _actualizationCts;
    
    public BackendDepActualizerViewModel()
    {
        foreach (var csharpSolution in SolutionScanner.CsharpSolutions
                     .Where(x => x is { IsCorporate: true })
                     .OrderBy(x => x.IsRunnable)
                     .ThenBy(x => x.Name))
        {
            ActualizeSolutions.Add(new()
            {
                Solution = csharpSolution
            });
        }
    }

    #region Dotnet Tool Init
    
    public async Task InitDotnetToolAsync(CancellationToken ct = default)
    {
        if (IsInitialized == true || IsProcessing == true)
            return;
        
        IsProcessing = true;

        try
        {
            if (await DotnetToolHelper.IsToolInstalled(ToolName, ct))
            {
                // RunDotnetCommand($"tool update --global {ToolName}");
                IsInitialized = true;
            }
            else
            {
                if (!await DotnetToolHelper.RunDotnetCommand($"tool install --global {ToolName}", ct))
                {
                    IsInitialized = false;
                }
                else
                {
                    IsInitialized = await DotnetToolHelper.IsToolInstalled(ToolName, ct);
                }
            }
        }
        catch
        {
            IsInitialized = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    
    
    #endregion Dotnet Tool Init
    
    private void LogActualize(string message)
    {
        ActualizeResult += $"{DateTime.Now}: {message}\n";
    }

    [RelayCommand]
    private void UseDagFilterSelection()
    {
        foreach (var solution in ActualizeSolutions)
        {
            solution.IsChecked = false;
        }

        if (DagFilterSolution == null)
            return;

        var graph = DependencyGraphBuilder.Build(ActualizeSolutions.Select(x => x.Solution).ToArray());
        var layers = GraphLayering.BuildLayers(graph);
        var visible = GraphQueries.GetDownstreamInclusive(graph, DagFilterSolution.Solution.Id);

        List<LevelVm> levels = [];
        for (var i = 0; i < layers.Count; i++)
        {
            var level = new LevelVm { Level = i };

            foreach (var id in layers[i])
            {
                var solution = graph.Nodes[id].Solution;

                level.Nodes.Add(solution);
            }

            levels.Add(level);
        }

        var filteredLevels = ApplyFilter(graph, levels, DagFilterSolution.Solution.Id);
        if (filteredLevels.Count < 2)
            return;

        foreach (var solution in filteredLevels[1].Nodes)
        {
            ActualizeSolutions.Single(x => x.Solution.Id == solution.Id).IsChecked = true;
        }
    }

    private List<LevelVm> ApplyFilter(DependencyGraph graph, ICollection<LevelVm> levels, Guid? libraryId)
    {
        var visible = libraryId is null
            ? null
            : GraphQueries.GetDownstreamInclusive(graph, libraryId.Value);

        List<LevelVm> filteredLevels = [];
        foreach (var lvl in levels)
        {
            var nodes = (visible is null)
                ? lvl.Nodes.ToList()
                : lvl.Nodes.Where(n => visible.Contains(n.Id)).ToList();

            if (nodes.Count == 0)
                continue; // hide empty levels

            var copy = new LevelVm { Level = lvl.Level };
            foreach (var n in nodes)
                copy.Nodes.Add(n);

            filteredLevels.Add(copy);
        }

        return filteredLevels;
    }

    [RelayCommand]
    private void InterruptFrontendDepsActualization()
    {
        _actualizationCts?.Cancel();
    }

    [RelayCommand]
    private async Task ActualizeFrontendDeps()
    {
        _actualizationCts = new CancellationTokenSource();
        CanInterruptActualization = true;

        try
        {
            ActualizeResult = string.Empty;
            ActualizeViewEnabled = false;

            foreach (var solutionVm in ActualizeSolutions.Where(x => x.IsChecked))
            {
                if (_actualizationCts.IsCancellationRequested)
                {
                    LogActualize("Прервано");
                    return;
                }

                try
                {
                    solutionVm.IsProcessing = true;

                    var solution = solutionVm.Solution;
                    switch (solution)
                    {
                        case null:
                            LogActualize("Не выбран проект\n\n");
                            continue;
                        case { IsPackable: true, TagVersionNumber: null }:
                            LogActualize("У библиотеки не определена текущая версия\n\n");
                            continue;
                    }

                    LogActualize($"Начало актуализации {solution.Name}...");

                    LogActualize("Выполнение [dotnet outdated]...");
                    if (!await TerminalHelper.RunCmd(Command, CommandArgs, solution.Path, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [dotnet outdated]\n\n");
                        continue;
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        return;
                    }

                    using (var repo = new Repository(solution.Path))
                    {
                        Commands.Stage(repo, ".");
                        if (!repo.RetrieveStatus().IsDirty)
                        {
                            LogActualize("Нечего обновлять, пропускаю\n\n");
                            continue;
                        }
                    }

                    if (!SkipBuild)
                    {
                        LogActualize("Выполнение [dotnet build]...");
                        if (!await TerminalHelper.RunCmd("dotnet", "build", solution.Path, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при выполнении [dotnet build]\n\n");
                            continue;
                        }
                    }
                    else
                    {
                        LogActualize("Выполнение [dotnet build] пропускается...");
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        return;
                    }

                    var nextVersion = solution.IsPackable
                        ? VersionHelper.IncPatchVersion(solution.TagVersionNumber)
                        : null;
                    
                    if (solution.IsPackable)
                    {
                        LogActualize("Создание записи для changelog...");
                        var changelogFilename = Path.Combine(solution.Path, "changelog.md");
                        ChangelogHelper.AddVersion(changelogFilename,
                            nextVersion,
                            ["Обновление зависимостей"]);
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        return;
                    }

                    LogActualize("Создание комита...");
                    
                    const string defaultCommitMessage = "commit -m \"chore: bump deps\"";
                    var commitMessage = solution.IsPackable
                        ? $"{defaultCommitMessage} to {nextVersion}\""
                        : defaultCommitMessage;
                    
                    if (!await TerminalHelper.RunCmd("git", "add .", solution.Path, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка stage комита\n\n");
                        continue;
                    }

                    if (!await TerminalHelper.RunCmd("git", commitMessage, solution.Path, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при создании комита\n\n");
                        continue;
                    }

                    LogActualize("Готово!\n\n");
                }
                finally
                {
                    solutionVm.IsProcessing = false;
                }
            }
        }
        finally
        {
            ActualizeViewEnabled = true;
            CanInterruptActualization = false;

            _actualizationCts?.Dispose();
            _actualizationCts = null;
        }
    }
}