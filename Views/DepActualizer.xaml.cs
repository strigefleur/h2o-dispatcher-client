using System.Windows.Controls;
using Felweed.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class DepActualizer : UserControl, INavigableView<DepActualizerViewModel>
{
    public DepActualizerViewModel ViewModel { get; }
    
    public DepActualizer()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<DepActualizerViewModel>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}