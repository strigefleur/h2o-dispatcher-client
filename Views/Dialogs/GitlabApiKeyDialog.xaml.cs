using System.Windows.Controls;
using Felweed.ViewModels.Dialogs;

namespace Felweed.Views.Dialogs;

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