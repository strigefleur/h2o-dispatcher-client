using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels.MainMenu.Settings.MiscSettings;

public partial class MiscSettingVm : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string? Value { get; set; }
    
    public required string AppConfigPropName { get; set; }
}