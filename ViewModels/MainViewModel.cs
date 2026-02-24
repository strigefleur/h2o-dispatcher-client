using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;

namespace Felweed.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Solution> _solutions = [];
    
    [ObservableProperty] private bool _isLoading;

    public MainViewModel()
    {
        _ = InitializeAsync();
    }

    [RelayCommand]
    private void RunSolution(Solution? solution)
    {
        solution?.Run();
    }
    
    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var scanner = await SolutionScanner.ScanAsync([@"D:\dev\rshb\h2o"]);
            List<Solution> solutions = [..scanner.CsharpSolutions, ..scanner.AngularSolutions];
            
            Solutions = new ObservableCollection<Solution>(solutions.OrderByDescending(x => x.Type));
        }
        finally
        {
            IsLoading = false;
        }
    }
}