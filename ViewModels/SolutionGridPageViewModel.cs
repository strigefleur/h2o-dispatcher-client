using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;

namespace Felweed.ViewModels;

public partial class SolutionGridPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Solution> _solutions = [];

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