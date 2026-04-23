using System.Windows.Input;
using Felweed.Models.Enumerators;
using Felweed.ViewModels;
using Felweed.Views.MainMenu.About;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace Felweed.Views;

public partial class MainWindow
{
    public MainWindow(
        MainViewModel viewModel,
        INavigationViewPageProvider pageProvider, 
        INavigationService navigationService,
        IContentDialogService contentDialogService)
    {
        InitializeComponent();

        DataContext = viewModel;
        
        RootNavigation.SetPageProviderService(pageProvider);
        navigationService.SetNavigationControl(RootNavigation);
        
        contentDialogService.SetDialogHost(RootContentDialogHost);
    }

    private async void MainWindow_OnContentRendered(object? sender, EventArgs e)
    {
        await (DataContext as MainViewModel).InitializeAsync();
        
        RootNavigation.Navigate(typeof(AboutPage));
    }

    private void BackendGraphPage_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        MainViewModel.GraphPageSelector = SolutionKind.CSharp;
    }

    private void FrontendGraphPage_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        MainViewModel.GraphPageSelector = SolutionKind.Angular;
    }
}