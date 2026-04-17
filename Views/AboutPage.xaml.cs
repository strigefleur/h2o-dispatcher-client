using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class AboutPage : Page, INavigableView<AboutPageViewModel>
{
    public AboutPageViewModel ViewModel { get; }
    
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = ViewModel;
        
        Loaded += PageLoaded;
    }
    
    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.GetAnecdoteAsync();
    }
}