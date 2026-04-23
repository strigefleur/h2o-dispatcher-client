using System.Windows;
using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class ScriptPage : Page, INavigableView<ScriptPageViewModel>
{
    public ScriptPageViewModel ViewModel { get; }

    public ScriptPage(ScriptPageViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = viewModel;

        Loaded += PageLoaded;
    }

    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitAsync();
    }
}