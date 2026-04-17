using System.Windows.Controls;
using Felweed.Models;
using Felweed.Services;
using Felweed.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class EnvVariablesPage : Page, INavigableView<EnvVariablesPageViewModel>
{
    public EnvVariablesPageViewModel ViewModel { get; }
    
    public EnvVariablesPage(EnvVariablesPageViewModel viewModel)
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
        
        config.EnvVariables.Clear();
        config.EnvVariables.AddRange(ViewModel.EnvVariables.Select(x => new EnvVariable(x.Name, x.Value)));
        
        ConfigurationService.SaveConfig();
    }
}