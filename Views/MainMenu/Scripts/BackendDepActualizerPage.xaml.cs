using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class BackendDepActualizerPage : UserControl, INavigableView<BackendDepActualizerPageVm>
{
    public BackendDepActualizerPageVm ViewModel { get; }
    
    public BackendDepActualizerPage()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BackendDepActualizerPageVm>();

        ViewModel = viewModel;
        DataContext = ViewModel;
        
        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitAsync();
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
    
    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Mark the event as handled to prevent selection
        e.Handled = true;

        // Manually trigger the context menu if it's not opening automatically
        if (sender is ListViewItem { ContextMenu: not null } item)
        {
            item.ContextMenu.PlacementTarget = item;
            item.ContextMenu.IsOpen = true;
        }
    }
}