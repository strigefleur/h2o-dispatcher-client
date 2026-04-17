using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class DepActualizer : UserControl
{
    private DepActualizerViewModel Vm => (DepActualizerViewModel)DataContext;
    
    public DepActualizer()
    {
        InitializeComponent();

        DataContext = new DepActualizerViewModel();
    }
    
    private void ActualizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizeTextBox.ScrollToEnd();
    }
}