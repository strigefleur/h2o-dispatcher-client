using System.Windows.Controls;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class GraphPage : Page
{
    public GraphPage()
    {
        InitializeComponent();

        DataContext = new GraphPageViewModel();
    }
}