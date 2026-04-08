using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels;

public partial class MiscSettingVm : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string? _value;
    
    public required string AppConfigPropName { get; set; }
}