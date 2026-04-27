using System.Windows.Controls;
using Felweed.ViewModels.Dialogs;

namespace Felweed.Views.Dialogs;

public partial class ActualizerResultDialog : UserControl
{
    public ActualizerResultDialogVm ViewModel { get; }
    
    public ActualizerResultDialog(string text)
    {
        InitializeComponent();
        
        ViewModel = new ActualizerResultDialogVm()
        {
            Text = text
        };
        DataContext = ViewModel;
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}