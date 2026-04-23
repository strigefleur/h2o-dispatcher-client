using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Models;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.MainMenu.Scripts;

public partial class SolutionActualizeVm : ObservableObject
{
    [ObservableProperty]
    public partial Solution? Solution { get; set; }

    [ObservableProperty]
    public partial bool? IsProcessing { get; set; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    [ObservableProperty] public partial SolutionActualizeStatus Status { get; set; } = SolutionActualizeStatus.None;

    public void ResetStatus()
    {
        Status = SolutionActualizeStatus.None;
        IsProcessing = false;
    }
}