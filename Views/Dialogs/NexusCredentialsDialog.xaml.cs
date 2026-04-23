using System.Windows.Controls;
using Felweed.ViewModels.Dialogs;

namespace Felweed.Views.Dialogs;

public partial class NexusCredentialsDialog : UserControl
{
    public NexusCredentialsDialogVm ViewModel { get; }
    
    public NexusCredentialsDialog()
    {
        InitializeComponent();
        
        ViewModel = new NexusCredentialsDialogVm();
        DataContext = ViewModel;
    }
}