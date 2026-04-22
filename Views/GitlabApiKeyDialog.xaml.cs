using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class GitlabApiKeyDialog : UserControl
{
    public GitlabApiKeyDialogVm ViewModel { get; }
    
    public GitlabApiKeyDialog()
    {
        InitializeComponent();
        
        ViewModel = new GitlabApiKeyDialogVm();
        DataContext = ViewModel;
    }
}