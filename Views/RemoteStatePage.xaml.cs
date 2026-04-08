using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class RemoteStatePage : Page
{
    private RemoteStatePageViewModel Vm => (RemoteStatePageViewModel)DataContext;
    
    public RemoteStatePage()
    {
        InitializeComponent();

        DataContext = new RemoteStatePageViewModel();

        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await Vm.ConnectAsync();
    }
}