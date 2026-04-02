using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels;

public partial class EnvVariableVm : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _value = "";
}