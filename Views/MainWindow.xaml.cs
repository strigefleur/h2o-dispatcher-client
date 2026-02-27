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
}