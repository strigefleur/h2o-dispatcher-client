using System.Collections.ObjectModel;
using System.IO;
using Ardalis.GuardClauses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Extensions;
using Felweed.Models;
using Felweed.Models.Enumerators;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;
using Felweed.Views.Dialogs;
using LibGit2Sharp;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Felweed.ViewModels.MainMenu.Scripts;

public partial class BackendDepActualizerPageVm : ObservableObject
{
    private readonly IContentDialogService _contentDialogService;
    private readonly ISnackbarService _snackbarService;
    
    [ObservableProperty]
    public partial bool? IsInitialized { get; set; }

    [ObservableProperty]
    public partial bool? IsProcessing { get; set; }

    [ObservableProperty]
    public partial string? InitError { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SolutionActualizeVm> ActualizeSolutions { get; set; } = [];
    
    [ObservableProperty]
    public partial bool SkipBuild { get; set; }

    [ObservableProperty]
    public partial string ActualizeResult { get; set; } = "";

    [ObservableProperty]
    public partial bool ActualizeViewEnabled { get; set; } = true;
    
    [ObservableProperty]
    public partial bool IncludePreRelease { get; set; }

    [ObservableProperty]
    public partial bool CanInterruptActualization { get; set; }

    [ObservableProperty]
    public partial SolutionActualizeVm? DagFilterSolution { get; set; }

    private CancellationTokenSource? _actualizationCts;
    
    public BackendDepActualizerPageVm(IContentDialogService contentDialogService, ISnackbarService snackbarService)
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        
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
    
    #region Init

    public async Task InitAsync()
    {
        if (IsInitialized == true || IsProcessing == true)
            return;
        
        IsProcessing = true;
        
        try
        {
            if (!NugetHelper.IsValidNugetFeedConfig())
            {
                IsInitialized = false;
                InitError = "В настройках не задана конфигурация Nuget";
                
                return;
            }
            
            await InitDotnetToolAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize backend dep actualizer");
            InitError = $"Ошибка при инициализации конфигурации Nuget";
            IsInitialized = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    private async Task InitDotnetToolAsync(CancellationToken ct = default)
    {
        // if (await DotnetToolHelper.IsToolInstalled(ToolName, ct))
        {
            // RunDotnetCommand($"tool update --global {ToolName}");
            IsInitialized = true;
        }
        // else
        // {
        //     if (!await DotnetToolHelper.RunDotnetCommand($"tool install --global {ToolName}", ct))
        //     {
        //         InitError = $"Не удалось установить утилиту {ToolName}";
        //         IsInitialized = false;
        //     }
        //     else
        //     {
        //         IsInitialized = await DotnetToolHelper.IsToolInstalled(ToolName, ct);
        //         if (IsInitialized != true)
        //         {
        //             InitError = $"Что-то пошло не так при установке утилиты {ToolName}";
        //         }
        //     }
        // }
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

        var graph = DependencyGraphBuilder.Build(ActualizeSolutions.Select<SolutionActualizeVm, Solution>(x => x.Solution).ToArray());
        var layers = GraphLayering.BuildLayers(graph);
        var visible = GraphQueries.GetDownstreamInclusive(graph, DagFilterSolution.Solution.Id);

        List<Graph.LevelVm> levels = [];
        for (var i = 0; i < layers.Count; i++)
        {
            var level = new Graph.LevelVm { Level = i };

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

    private List<Graph.LevelVm> ApplyFilter(DependencyGraph graph, ICollection<Graph.LevelVm> levels, Guid? libraryId)
    {
        if (libraryId == null) return levels.ToList();

        // 1. Находим всех, кто зависит от библиотеки (вниз по графу)
        var downstreamIds = GraphQueries.GetDownstreamInclusive(graph, libraryId.Value).ToHashSet();

        // 2. Рассчитываем уровни ЛОКАЛЬНО относительно libraryId
        // libraryId = Level 0, его прямые потребители = Level 1, и т.д.
        var localLevels = new Dictionary<Guid, int>
        {
            [libraryId.Value] = 0
        };

        // Считаем Longest Path внутри подграфа
        var sorted = GraphLayering.TopoSortLocal(graph, downstreamIds);
        foreach (var id in sorted)
        {
            if (!graph.Outgoing.TryGetValue(id, out var edges)) continue;
            foreach (var edgeToId in edges.Select(x => x.ToId))
            {
                if (!downstreamIds.Contains(edgeToId)) continue;

                var currentLevel = localLevels.GetValueOrDefault(id, 0);
                var targetLevel = currentLevel + 1;

                if (!localLevels.TryGetValue(edgeToId, out var value) || value < targetLevel)
                    localLevels[edgeToId] = targetLevel;
            }
        }

        // 3. Собираем результат
        return localLevels
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var levelVm = new Graph.LevelVm { Level = g.Key };
                foreach (var kv in g)
                    levelVm.Nodes.Add(graph.Nodes[kv.Key].Solution);
                return levelVm;
            })
            .ToList();
    }

    [RelayCommand]
    private void InterruptBackendDepsActualization()
    {
        _actualizationCts?.Cancel();
    }

    [RelayCommand]
    private async Task ActualizeBackendDeps()
    {
        _actualizationCts = new CancellationTokenSource();
        CanInterruptActualization = true;
        
        foreach (var solution in ActualizeSolutions)
        {
            solution.ResetStatus();
        }

        var ignoredAsDepList = ActualizeSolutions
            .Where(x => x.IsIgnoredAsDep)
            .Select(x => x.Solution.PackageId)
            .ToArray();
        
        ActualizeResult = string.Empty;
        ActualizeViewEnabled = false;

        try
        {
            var config = ConfigurationService.LoadConfig();
            var gitlabToken = SecureStorage.LoadApiKey();
            if (gitlabToken == null)
                return;

            foreach (var solutionVm in ActualizeSolutions.Where(x => x.IsChecked))
            {
                if (_actualizationCts.IsCancellationRequested)
                {
                    LogActualize("Прервано");
                    solutionVm.Status = SolutionActualizeStatus.Skipped;
                    return;
                }
                
                solutionVm.Status = SolutionActualizeStatus.InProgress;

                try
                {
                    solutionVm.IsProcessing = true;

                    var solution = solutionVm.Solution;
                    switch (solution)
                    {
                        case null:
                            LogActualize("Не выбран проект\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Skipped;
                            continue;
                        case { IsPackable: true, TagVersionNumber: null }:
                            LogActualize("У библиотеки не определена текущая версия\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Skipped;
                            continue;
                    }

                    LogActualize($"Начало актуализации {solution.Name}...");
                    
                    var dir = Guard.Against.Null(Path.GetDirectoryName(solution.Path));
                    using var repo = new Repository(dir);
                    
                    var branch = config.ActiveProfile.ActiveBranch;
                    if (string.IsNullOrWhiteSpace(branch))
                    {
                        LogActualize("Ошибка при определении активной ветки\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }
                    
                    LogActualize($"Выполнение [git pull] для {branch}...");
                    if (!await repo.PullAsync(gitlabToken, dir, branch, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [git pull]\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }
                    
                    LogActualize("Выполнение [dotnet restore]...");
                    if (!await TerminalHelper.DotnetRestoreAsync(dir, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [git restore]\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    LogActualize("Выполнение [dotnet outdated]...");
                    if (!await NugetHelper.UpdatePackagesAsync(solutionVm.Solution as CSharpSolution, ignoredAsDepList,
                            _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [dotnet outdated]\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        return;
                    }

                    Commands.Stage(repo, ".");
                    if (!repo.RetrieveStatus().IsDirty)
                    {
                        LogActualize("Нечего обновлять, пропускаю\n\n");
                        
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        continue;
                    }

                    if (!SkipBuild)
                    {
                        LogActualize("Выполнение [dotnet build]...");
                        if (!await TerminalHelper.RunCmd("dotnet", "build", dir, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при выполнении [dotnet build]\n\n");
                            
                            solutionVm.Status = SolutionActualizeStatus.Failed;
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
                        
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        return;
                    }
                    
                    // после пула могли появиться новые тэги, нужен повторный анализ
                    solution.UpdateTagVersionNumber(repo.GetLatestTagVersion());

                    var nextVersion = solution.IsPackable
                        ? VersionHelper.IncPatchVersion(solution.TagVersionNumber)
                        : null;

                    if (solution.IsPackable)
                    {
                        LogActualize("Создание записи для changelog...");
                        var changelogFilename = Path.Combine(dir, "changelog.md");
                        ChangelogHelper.AddVersion(changelogFilename,
                            nextVersion,
                            ["Обновление зависимостей"]);
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        return;
                    }

                    LogActualize("Создание комита...");

                    const string defaultCommitMessage = "chore: bump deps";
                    var commitMessage = solution.IsPackable
                        ? $"{defaultCommitMessage} to {nextVersion}"
                        : defaultCommitMessage;
                    
                    if (!await repo.StageAndCommitAsync(dir, commitMessage, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка stage/commit\n\n");
                        
                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    solutionVm.Status = SolutionActualizeStatus.Success;
                    LogActualize("Готово!\n\n");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error actualizing backend deps");
                    // solutionVm.Status = SolutionActualizeStatus.Failed;
                }
                finally
                {
                    solutionVm.IsProcessing = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to actualize backend deps");
        }
        finally
        {
            ActualizeViewEnabled = true;
            CanInterruptActualization = false;

            _actualizationCts?.Dispose();
            _actualizationCts = null;
        }
    }
    
    [RelayCommand]
    private async Task ShowResultDialog()
    {
        await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions()
            {
                Title = $"Результаты актуализации на {DateTime.Now:G}",
                Content = new ActualizerResultDialog(ActualizeResult),
                PrimaryButtonText = "Ок",
                CloseButtonText = "Ну, ок"
            }
        );
    }

    [RelayCommand]
    private async Task ClearNugetCache(CancellationToken ct = default)
    {
        ActualizeViewEnabled = false;

        try
        {
            var isOk = await TerminalHelper.NugetClearCacheAsync(AppDomain.CurrentDomain.BaseDirectory, ct);
            var textResult = isOk ? "сброшен" : "не удалось сбросить";
        
            _snackbarService.Show(
                "Nuget",
                $"Локальный HTTP-кэш Nuget {textResult}",
                ControlAppearance.Secondary,
                new SymbolIcon(SymbolRegular.Fluent24),
                TimeSpan.FromSeconds(3)
            );
        }
        finally
        {
            ActualizeViewEnabled = true;
        }
    }
}