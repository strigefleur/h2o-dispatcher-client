using System.Windows.Controls;
using Felweed.ViewModels.MainMenu.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views.MainMenu.Scripts;

public partial class BatchRepoTextReplacePage : UserControl, INavigableView<BatchRepoTextReplacePageVm>
{
    public BatchRepoTextReplacePageVm ViewModel { get; }
    
    public BatchRepoTextReplacePage()
    {
        InitializeComponent();
        
        var viewModel = App.Current.ServiceProvider.GetRequiredService<BatchRepoTextReplacePageVm>();

        ViewModel = viewModel;
        DataContext = ViewModel;
    }
}