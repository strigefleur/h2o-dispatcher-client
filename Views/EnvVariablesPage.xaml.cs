using System.Windows.Controls;
using Felweed.Models;
using Felweed.Services;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class EnvVariablesPage : Page
{
    private EnvVariablesPageViewModel Vm => (EnvVariablesPageViewModel)DataContext;
    
    public EnvVariablesPage()
    {
        InitializeComponent();

        DataContext = new EnvVariablesPageViewModel();
    }

    private void DataGrid_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.Cancel)
            return;

        var config = ConfigurationService.LoadConfig();
        
        config.EnvVariables.Clear();
        config.EnvVariables.AddRange(Vm.EnvVariables.Select(x => new EnvVariable(x.Name, x.Value)));
        
        ConfigurationService.SaveConfig();
    }
}