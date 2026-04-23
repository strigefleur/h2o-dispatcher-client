using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.RemoteState;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.RemoteState;

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