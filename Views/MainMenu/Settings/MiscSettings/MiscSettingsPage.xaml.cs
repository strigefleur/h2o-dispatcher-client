using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Settings.MiscSettings;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Settings.MiscSettings;

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

        if (e.Row.DataContext is not MiscSettingVm miscSettingVm)
            return;

        ViewModel.OnSettingVmUpdated(miscSettingVm);
    }
}