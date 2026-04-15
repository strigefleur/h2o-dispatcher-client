using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class ScriptPage : Page
{
    private ScriptPageViewModel Vm => (ScriptPageViewModel)DataContext;
    
    public ScriptPage()
    {
        InitializeComponent();

        DataContext = new ScriptPageViewModel();
    }

    private void FrontendServiceCb_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Vm.ActualizeResult = string.Empty;
    }
}