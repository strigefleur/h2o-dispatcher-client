using System.Windows;
using Felweed.Services;
using Felweed.ViewModels;
using Felweed.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Felweed;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    
    public App()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<ConfigurationService>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Windows
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        
        mainWindow.Show();
    }
}