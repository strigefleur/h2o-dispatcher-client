using System.Windows.Controls;
using Felweed.Services;
using Felweed.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class MiscSettingsPage : Page, INavigableView<MiscSettingsPageViewModel>
{
    public MiscSettingsPageViewModel ViewModel { get; }
    
    public MiscSettingsPage(MiscSettingsPageViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void DataGrid_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.Cancel)
            return;

        var config = ConfigurationService.LoadConfig();

        foreach (var miscSetting in ViewModel.MiscSettings)
        {
            config[miscSetting.AppConfigPropName] = miscSetting.Value;
        }
        
        ConfigurationService.SaveConfig();
    }
}