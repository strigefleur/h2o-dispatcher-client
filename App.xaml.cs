using System.Windows;
using Felweed.Services;
using Felweed.ViewModels;
using Felweed.Views;
using Microsoft.Extensions.DependencyInjection;
using Velopack;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Felweed;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    
    public App()
    {
        var services = new ServiceCollection();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Windows
        services.AddTransient<MainWindow>();
        
        services.AddHttpClient();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        await Updater.UpdateAsync();
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        ApplyThemeFromConfig();
        
        mainWindow.Show();
    }

    private void ApplyThemeFromConfig()
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        var config = ConfigurationService.LoadConfig();

        var configTheme = config.ThemeSwitchIcon == SymbolRegular.WeatherMoon24
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        if (configTheme == currentTheme)
            return;

        ApplicationThemeManager.Apply(
            currentTheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark,
            WindowBackdropType.None);
    }
    
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        App app = new();
        app.InitializeComponent();
        app.Run();
    }
}