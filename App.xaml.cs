using System.IO;
using System.Windows;
using Felweed.Services;
using Felweed.ViewModels;
using Felweed.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Velopack;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;

namespace Felweed;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; }
    
    public new static App Current => (App)Application.Current;
    
    public App()
    {
        // Recommended path for logs
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "Felweed", 
            "Logs"
        );

        // Create the directory if it doesn't exist
        Directory.CreateDirectory(logDirectory);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File($"{logDirectory}/felweed-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        DispatcherUnhandledException += (s, e) => {
            Log.Fatal(e.Exception, "UI Thread crash");
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            Log.Fatal((Exception)e.ExceptionObject, "Background thread crash");
        };
        
        var services = new ServiceCollection();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Windows
        services.AddTransient<MainWindow>();

        services.AddTransient<SolutionGridPageViewModel>();
        services.AddTransient<SolutionGridPage>();

        services.AddTransient<ScriptPageViewModel>();
        services.AddTransient<ScriptPage>();

        services.AddSingleton<RemoteStatePageViewModel>();
        services.AddSingleton<RemoteStatePage>();
        
        services.AddTransient<MiscSettingsPageViewModel>();
        services.AddTransient<MiscSettingsPage>();

        services.AddTransient<GraphPageViewModel>();
        services.AddTransient<GraphPage>();

        services.AddTransient<EnvVariablesPageViewModel>();
        services.AddTransient<EnvVariablesPage>();

        services.AddTransient<BatchRepoActionViewModel>();
        services.AddTransient<BatchRepoAction>();

        services.AddTransient<AboutPageViewModel>();
        services.AddTransient<AboutPage>();
        
        services.AddSingleton<FrontendDepActualizerViewModel>();
        services.AddSingleton<FrontendDepActualizer>();
        
        services.AddSingleton<BackendDepActualizerViewModel>();
        services.AddSingleton<BackendDepActualizer>();

        services.AddTransient<BatchRepoCheckoutVm>();
        services.AddTransient<BatchRepoCheckout>();
        
        services.AddNavigationViewPageProvider();
        services.AddSingleton<INavigationService, NavigationService>();
        
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        
        services.AddHttpClient();

        ServiceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        await Updater.UpdateAsync();
        
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        ApplyThemeFromConfig();
        
        mainWindow.Show();
    }

    private static void ApplyThemeFromConfig()
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
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}