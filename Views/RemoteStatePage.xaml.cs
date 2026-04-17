using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class RemoteStatePage : Page, INavigableView<RemoteStatePageViewModel>
{
    public RemoteStatePageViewModel ViewModel { get; }
    
    public RemoteStatePage(RemoteStatePageViewModel viewModel)
    {
        InitializeComponent();
        
        ViewModel = viewModel;
        DataContext = viewModel;

        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.ConnectAsync();
    }
}