using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Constants;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;
using LibGit2Sharp;

namespace Felweed.ViewModels;

public partial class DepActualizerViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<SolutionActualizeVm> _actualizeSolutions = [];
    [ObservableProperty] private bool _skipBuild;
    [ObservableProperty] private string _actualizeResult = "";
    [ObservableProperty] private bool _actualizeViewEnabled = true;
    [ObservableProperty] private bool _canInterruptActualization;
    [ObservableProperty] private SolutionActualizeVm? _dagFilterSolution;

    private CancellationTokenSource? _actualizationCts;

    public DepActualizerViewModel()
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

                    LogActualize("Выполнение [npm-check-updates]...");
                    const string angularDepPrefixRegex =
                        @$"/^{PrefixConst.AngularCorporateL0Prefix}\.{PrefixConst.AngularCorporateL1Prefix}\//";

                    if (!await TerminalHelper.RunCmd("npx",
                            @$"--strict-ssl=false npm-check-updates -p yarn -f {angularDepPrefixRegex} -u --install always",
                            solution.Path, _actualizationCts.Token))
                    {
                        LogActualize("Ошибка при выполнении [npm-check-updates]\n\n");
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
                        LogActualize("Выполнение [yarn build]...");
                        if (!await TerminalHelper.RunCmd("yarn", "build", solution.Path, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при выполнении [yarn build]\n\n");
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
                        return;
                    }

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
                            return;
                        }

                        LogActualize("Создание записи для changelog...");
                        var changelogFilename = Path.Combine(solution.Path, "changelog.md");
                        ChangelogHelper.AddVersion(changelogFilename,
                            VersionHelper.IncPatchVersion(solution.TagVersionNumber),
                            ["Обновление зависимостей"]);
                    }

                    if (_actualizationCts.IsCancellationRequested)
                    {
                        LogActualize("Прервано");
                        return;
                    }

                    LogActualize("Создание комита...");
                    if (solution.IsPackable)
                    {
                        if (!await TerminalHelper.RunCmd("python", "bump_version.py", solution.Path,
                                _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при создании комита\n\n");
                            continue;
                        }
                    }
                    else
                    {
                        if (!await TerminalHelper.RunCmd("git", "add .", solution.Path, _actualizationCts.Token))
                        {
                            LogActualize("Ошибка stage комита\n\n");
                            continue;
                        }

                        if (!await TerminalHelper.RunCmd("git", "commit -m \"chore: bump deps\"", solution.Path,
                                _actualizationCts.Token))
                        {
                            LogActualize("Ошибка при создании комита\n\n");
                            continue;
                        }
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