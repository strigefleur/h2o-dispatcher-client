using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class BackendDepActualizer : UserControl, INavigableView<BackendDepActualizerViewModel>
{
    public BackendDepActualizerViewModel ViewModel { get; }
    
    public BackendDepActualizer()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BackendDepActualizerViewModel>();

        ViewModel = viewModel;
        DataContext = ViewModel;
        
        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitDotnetToolAsync();
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}