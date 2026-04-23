using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class FrontendDepActualizerPage : UserControl, INavigableView<FrontendDepActualizerPageVm>
{
    public FrontendDepActualizerPageVm ViewModel { get; }
    
    public FrontendDepActualizerPage()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<FrontendDepActualizerPageVm>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}