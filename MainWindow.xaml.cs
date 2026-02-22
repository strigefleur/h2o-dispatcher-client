using System.Windows;
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