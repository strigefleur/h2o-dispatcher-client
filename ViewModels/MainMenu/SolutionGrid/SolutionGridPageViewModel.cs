using System.Collections.ObjectModel;
using System.IO;
using CliWrap;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Models.Enumerators;
using Felweed.Services;

namespace Felweed.ViewModels.MainMenu.SolutionGrid;

public partial class SolutionGridPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<Solution> Solutions { get; set; }

    public SolutionGridPageViewModel()
    {
        List<Solution> solutions = [..SolutionScanner.CsharpSolutions, ..SolutionScanner.AngularSolutions];

        Solutions = new ObservableCollection<Solution>(solutions.OrderByDescending(x => x.Type));
    }

    [RelayCommand]
    private void RunSolution(Solution? solution)
    {
        var config = ConfigurationService.LoadConfig();
        
        solution?.Run(config.ActiveProfile.CSharpCorporateL1Prefix);
    }

    [RelayCommand]
    private async Task OpenSolutionDir(Solution? solution)
    {
        if (solution == null)
            return;
        
        var solutionDir = solution.Kind == SolutionKind.Angular
            ? solution.Path
            : Path.GetDirectoryName(solution.Path);
        
        await Cli.Wrap("explorer.exe")
            .WithArguments(solutionDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }
}