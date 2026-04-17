using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class BatchRepoAction : UserControl
{
    private BatchRepoActionViewModel Vm => (BatchRepoActionViewModel)DataContext;
    
    public BatchRepoAction()
    {
        InitializeComponent();

        DataContext = new BatchRepoActionViewModel();
    }
}