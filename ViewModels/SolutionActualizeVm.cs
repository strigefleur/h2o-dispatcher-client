using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Models;

namespace Felweed.ViewModels;

public partial class SolutionActualizeVm : ObservableObject
{
    [ObservableProperty] private Solution? _solution;
    [ObservableProperty] private bool? _isProcessing;
    [ObservableProperty] private bool _isChecked;
}