using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels.MainMenu.Settings.EnvVariables;

public partial class EnvVariableVm : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Value { get; set; } = "";
}