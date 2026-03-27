using System.Windows.Controls;
using System.Windows.Input;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class SolutionGridPage : Page
{
    public SolutionGridPage()
    {
        InitializeComponent();

        DataContext = new SolutionGridPageViewModel();
    }
    
    private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Stops the DataGrid from selecting the row or toggling details
        e.Handled = true;

        // Manually trigger the command since we handled the event
        var btn = sender as Button;
        if (btn?.Command != null && btn.Command.CanExecute(btn.CommandParameter))
        {
            btn.Command.Execute(btn.CommandParameter);
        }
    }
}