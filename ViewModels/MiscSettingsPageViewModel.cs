using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Services;

namespace Felweed.ViewModels;

public partial class MiscSettingsPageViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<MiscSettingVm> _miscSettings = [];

    public MiscSettingsPageViewModel()
    {
        var config = ConfigurationService.LoadConfig();

        MiscSettings.Add(new()
        {
            Name = "Адрес Gitlab",
            Value = config.ServerUrl,
            AppConfigPropName = nameof(config.ServerUrl),
        });
    }
}