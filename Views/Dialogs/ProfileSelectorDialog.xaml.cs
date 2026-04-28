using System.Windows.Controls;
using Felweed.ViewModels.Dialogs;

namespace Felweed.Views.Dialogs;

public partial class ProfileSelectorDialog : UserControl
{
    public ProfileSelectorDialogVm ViewModel { get; }
    
    public ProfileSelectorDialog()
    {
        InitializeComponent();
        
        ViewModel = new ProfileSelectorDialogVm();
        DataContext = ViewModel;
    }
}