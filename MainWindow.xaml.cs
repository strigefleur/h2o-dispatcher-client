using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Felweed.Services;

namespace Felweed;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var scaner = await SolutionScanner.ScanAsync([@"D:\dev\rshb\h2o"]);
        }
        catch (Exception ex)
        {
            throw; // TODO handle exception
        }
    }
}