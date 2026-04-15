using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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

    [ObservableProperty] private ObservableCollection<SolutionActualizeVm> _actualizeSolutions = [];
    [ObservableProperty] private bool _skipBuild;
    [ObservableProperty] private string _actualizeResult = "";
    [ObservableProperty] private bool _actualizeViewEnabled = true;
    [ObservableProperty] private bool _canInterruptActualization;

    private CancellationTokenSource? _actualizationCts;

    #endregion

    public ScriptPageViewModel()
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

    private void LogActualize(string message)
    {
        ActualizeResult += $"{DateTime.Now}: {message}\n";
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
                    if (!await TerminalHelper.RunCmd("npx",
                            @"--strict-ssl=false npm-check-updates -p yarn -f /^@rshbgroup\.cfo\// -u --install always",
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
                        if (!await TerminalHelper.RunCmd("python", "bump_version.py", solution.Path, _actualizationCts.Token))
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