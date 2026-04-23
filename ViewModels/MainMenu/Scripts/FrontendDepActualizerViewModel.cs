using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Constants;
using Felweed.Extensions;
using Felweed.Models.Enumerators;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;
using LibGit2Sharp;
using Serilog;

namespace Felweed.ViewModels.MainMenu.Scripts;

public partial class FrontendDepActualizerViewModel : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<SolutionActualizeVm> ActualizeSolutions { get; set; } = [];

    [ObservableProperty] public partial bool SkipBuild { get; set; }

    [ObservableProperty] public partial string ActualizeResult { get; set; } = "";

    [ObservableProperty] public partial bool ActualizeViewEnabled { get; set; } = true;

    [ObservableProperty] public partial bool CanInterruptActualization { get; set; }

    [ObservableProperty] public partial SolutionActualizeVm? DagFilterSolution { get; set; }

    private CancellationTokenSource? _actualizationCts;

    public FrontendDepActualizerViewModel()
    {
        foreach (var angularSolution in SolutionScanner.AngularSolutions
                     .Where(x => x is { IsCorporate: true })
                     .OrderBy(x => x.IsRunnable)
                     .ThenBy(x => x.Name))
        {
            ActualizeSolutions.Add(new()
            {
                Solution = angularSolution
            });
        }
    }

    private void LogActualize(string message)
    {
        ActualizeResult += $"{DateTime.Now}: {message}\n";
    }

    [RelayCommand]
    private void UseDagFilterSelection()
    {
        foreach (var solution in ActualizeSolutions)
        {
            solution.ResetStatus();
        }

        if (DagFilterSolution == null)
            return;

        var graph = DependencyGraphBuilder.Build(ActualizeSolutions.Select(x => x.Solution).ToArray());
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

    private static List<Graph.LevelVm> ApplyFilter(DependencyGraph graph, ICollection<Graph.LevelVm> levels, Guid? libraryId)
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
    private void InterruptFrontendDepsActualization()
    {
        _actualizationCts?.Cancel();
    }

    [RelayCommand]
    private async Task ActualizeFrontendDeps()
    {
        _actualizationCts = new CancellationTokenSource();
        CanInterruptActualization = true;

        var config = ConfigurationService.LoadConfig();
        var gitlabToken = SecureStorage.LoadApiKey();
        foreach (var solution in ActualizeSolutions)
        {
            solution.ResetStatus();
        }

        try
        {
            ActualizeResult = string.Empty;
            ActualizeViewEnabled = false;

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
                            continue;
                        case { IsPackable: true, TagVersionNumber: null }:
                            LogActualize("У библиотеки не определена текущая версия\n\n");
                            continue;
                    }

                    LogActualize($"Начало актуализации {solution.Name}...");

                    string authUrl;
                    using (var prePullRepo = new Repository(solution.Path))
                    {
                        var remote = prePullRepo.GetRemoteUrl();
                        var cleanUrl = remote.Replace("https://", "");
                        authUrl = $"https://oauth2:{gitlabToken}@{cleanUrl}";
                    }

                    var branch = config.ActiveBranch;
                    if (string.IsNullOrWhiteSpace(branch))
                    {
                        LogActualize("Ошибка при определении активной ветки\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    LogActualize($"Выполнение [git pull] для {branch}...");
                    if (!await TerminalHelper.RunCmd("git", $"pull {authUrl} {branch} --ff-only --tags", solution.Path,
                            _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [git pull]\n\n");

                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    LogActualize("Выполнение [npm-check-updates]...");
                    const string angularDepPrefixRegex =
                        @$"/^{PrefixConst.AngularCorporateL0Prefix}\.{PrefixConst.AngularCorporateL1Prefix}\//";

                    if (!await TerminalHelper.RunCmd("npx",
                            @$"--strict-ssl=false -y npm-check-updates -p yarn -f {angularDepPrefixRegex} -u --install always",
                            solution.Path, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [npm-check-updates]\n\n");
                        solutionVm.Status = SolutionActualizeStatus.Failed;
                        continue;
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        return;
                    }

                    // заново инициализируем, чтобы гарантировать наличие запуленных тэгов
                    using var repo = new Repository(solution.Path);

                    Commands.Stage(repo, ".");
                    if (!repo.RetrieveStatus().IsDirty)
                    {
                        LogActualize("Нечего обновлять, пропускаю\n\n");
                        solutionVm.Status = SolutionActualizeStatus.Skipped;
                        continue;
                    }

                    if (!SkipBuild)
                    {
                        LogActualize("Выполнение [yarn build]...");
                        if (!await TerminalHelper.RunCmd("yarn", "build", solution.Path, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при выполнении [yarn build]\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                            continue;
                        }
                    }
                    else
                    {
                        LogActualize("Выполнение [yarn build] пропускается...");
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
                        LogActualize("Актуализация внутреннего package.json библиотеки...");

                        var rootPath = Path.Combine(solution.Path, "package.json");
                        var projectsDir = Path.Combine(solution.Path, "projects");
                        var projectDirName = Path.GetFileName(Directory.EnumerateDirectories(projectsDir).Single());
                        var innerPath = Path.Combine(solution.Path, "projects", projectDirName, "package.json");

                        var rootPkg = PackageJsonHelper.LoadPackageJson(rootPath);
                        var innerPkg = PackageJsonHelper.LoadPackageJson(innerPath);

                        var rootVersions = PackageJsonHelper.ReadVersions(rootPkg, "dependencies", "devDependencies",
                            "peerDependencies", "optionalDependencies");

                        var totalChanged1 = PackageJsonHelper.SyncSection(innerPkg, "dependencies", rootVersions);
                        var totalChanged2 = PackageJsonHelper.SyncSection(innerPkg, "devDependencies", rootVersions);
                        var totalChanged3 = PackageJsonHelper.SyncSection(innerPkg, "peerDependencies", rootVersions);
                        var totalChanged4 =
                            PackageJsonHelper.SyncSection(innerPkg, "optionalDependencies", rootVersions);

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        await File.WriteAllTextAsync(innerPath, innerPkg.ToJsonString(options));

                        var totalChanged = totalChanged1 + totalChanged2 + totalChanged3 + totalChanged4;
                        LogActualize(
                            $"Во внутренний package.json библиотеки перенесено {totalChanged} изменений версии");

                        if (_actualizationCts.IsCancellationRequested)
                        {
                            LogActualize("Прервано");
                            solutionVm.Status = SolutionActualizeStatus.Skipped;
                            return;
                        }

                        LogActualize("Создание записи для changelog...");
                        var changelogFilename = Path.Combine(solution.Path, "changelog.md");
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
                    if (solution.IsPackable)
                    {
                        if (!await TerminalHelper.RunCmd("python", "bump_version.py", solution.Path,
                                _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при создании комита\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                            continue;
                        }
                    }
                    else
                    {
                        if (!await TerminalHelper.RunCmd("git", "add .", solution.Path, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка stage комита\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                            continue;
                        }

                        if (!await TerminalHelper.RunCmd("git", "commit -m \"chore: bump deps\"", solution.Path,
                                _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при создании комита\n\n");
                            solutionVm.Status = SolutionActualizeStatus.Failed;
                            continue;
                        }
                    }

                    solutionVm.Status = SolutionActualizeStatus.Success;
                    LogActualize("Готово!\n\n");
                }
                finally
                {
                    solutionVm.IsProcessing = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error actualizing frontend deps");
            // solutionVm.Status = SolutionActualizeStatus.Failed;
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