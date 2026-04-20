using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

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