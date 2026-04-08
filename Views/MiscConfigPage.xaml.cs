using System.Windows.Controls;
using Felweed.Models;
using Felweed.Services;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class MiscConfigPage : Page
{
    private MiscSettingsPageViewModel Vm => (MiscSettingsPageViewModel)DataContext;
    
    public MiscConfigPage()
    {
        InitializeComponent();

        DataContext = new MiscSettingsPageViewModel();
    }

    private void DataGrid_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.Cancel)
            return;

        var config = ConfigurationService.LoadConfig();

        foreach (var miscSetting in Vm.MiscSettings)
        {
            config[miscSetting.AppConfigPropName] = miscSetting.Value;
        }
        
        ConfigurationService.SaveConfig();
    }
}