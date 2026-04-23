using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class BatchRepoAction : UserControl, INavigableView<BatchRepoActionViewModel>
{
    public BatchRepoActionViewModel ViewModel { get; }
    
    public BatchRepoAction()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BatchRepoActionViewModel>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
}