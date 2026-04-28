using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Services;

namespace Felweed.ViewModels.MainMenu.Settings.MiscSettings;

public partial class MiscSettingsPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<MiscSettingVm> MiscSettings { get; set; } = [];

    public MiscSettingsPageViewModel()
    {
        var config = ConfigurationService.LoadConfig();

        MiscSettings.Add(new()
        {
            Name = "Адрес сервера",
            Value = config.ActiveProfile.ServerUrl,
            AppConfigPropName = nameof(config.ActiveProfile.ServerUrl),
        });
    }
    
    public void OnSettingVmUpdated(MiscSettingVm vm)
    {
        var config = ConfigurationService.LoadConfig();
        config[vm.AppConfigPropName] = vm.Value;
        ConfigurationService.SaveConfig();
    }
}