using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class BatchRepoCheckoutPage : Page, INavigableView<BatchRepoCheckoutPageVm>
{
    public BatchRepoCheckoutPageVm ViewModel { get; }
    
    public BatchRepoCheckoutPage()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BatchRepoCheckoutPageVm>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
}