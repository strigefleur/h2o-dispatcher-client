using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
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
        
        solution?.Run([..config.CSharpSolutionPrefixes]);
    }
}