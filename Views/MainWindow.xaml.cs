using System.Windows.Input;
using Felweed.Models.Enumerators;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private async void MainWindow_OnContentRendered(object? sender, EventArgs e)
    {
        await (DataContext as MainViewModel).InitializeAsync();
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