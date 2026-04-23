using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class BatchRepoCheckout : UserControl, INavigableView<BatchRepoCheckoutVm>
{
    public BatchRepoCheckoutVm ViewModel { get; }
    
    public BatchRepoCheckout()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BatchRepoCheckoutVm>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
}