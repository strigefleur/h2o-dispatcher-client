using System.Windows.Controls;
using Felweed.ViewModels.Dialogs;

namespace Felweed.Views.Dialogs;

public partial class CredentialsDialog : UserControl
{
    public CredentialsDialogVm ViewModel { get; }
    
    public CredentialsDialog()
    {
        InitializeComponent();
        
        ViewModel = new CredentialsDialogVm();
        DataContext = ViewModel;
    }
}