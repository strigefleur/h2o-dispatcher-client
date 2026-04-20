using System.Windows.Controls;
using Felweed.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class FrontendDepActualizer : UserControl, INavigableView<FrontendDepActualizerViewModel>
{
    public FrontendDepActualizerViewModel ViewModel { get; }
    
    public FrontendDepActualizer()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<FrontendDepActualizerViewModel>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}