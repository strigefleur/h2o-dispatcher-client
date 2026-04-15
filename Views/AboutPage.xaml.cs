using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class AboutPage : Page
{
    private AboutPageViewModel Vm => (AboutPageViewModel)DataContext;
    
    public AboutPage()
    {
        InitializeComponent();

        DataContext = new AboutPageViewModel();
        
        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await Vm.GetAnecdoteAsync();
    }
}