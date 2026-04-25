using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models.Enumerators;
using Felweed.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public Dialogs.SetupDialogViewModel SetupDialogViewModel { get; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSelector { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial SymbolRegular ThemeSwitchIcon { get; set; } = ConfigurationService.LoadConfig().ThemeSwitchIcon;

    [ObservableProperty] public partial HubConnectionState ConnectionState { get; set; }

    // TODO
    [ObservableProperty]
    public partial bool ShowPublicDependencies { get; set; }
    public static SolutionKind GraphPageSelector { get; set; } = SolutionKind.CSharp;

    public MainViewModel()
    {
        ConnectionState = HubConnector.Connection?.State ?? HubConnectionState.Disconnected;
        HubConnector.StateChanged += OnSignalRStateChanged;
    }
    
    private void OnSignalRStateChanged(HubConnectionState newState)
    {
        App.Current.Dispatcher.Invoke(() => ConnectionState = newState);
    }
    
    partial void OnThemeSwitchIconChanged(SymbolRegular oldValue, SymbolRegular newValue)
    {
        var config = ConfigurationService.LoadConfig();
        config.ThemeSwitchIcon = newValue;
        
        ConfigurationService.SaveConfig();
    }
    
    [RelayCommand]
    private async Task ConfirmSelector()
    {
        await InitializeAsync();
    }
    
    [RelayCommand]
    private void SwitchTheme()
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();
    
        if (currentTheme == ApplicationTheme.Dark)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            ThemeSwitchIcon = SymbolRegular.WeatherMoon24;
        }
        else
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
            ThemeSwitchIcon = SymbolRegular.WeatherSunny24;
        }
    }

    private void ValidateDirectories()
    {
        var config = ConfigurationService.LoadConfig();
        
        if (!ConfigurationService.ValidateDirectories())
        {
            IsSelector = true;
            
            var selectedDirs = SetupDialogViewModel.SelectedPaths.ToList();
            if (selectedDirs.Count == 0)
            {
                return;
            }

            // Save new config
            config.SolutionDirectories = selectedDirs.ToList();
            ConfigurationService.SaveConfig();
        }

        IsSelector = false;
    }

    private async Task RunScanner()
    {
        IsLoading = true;
        
        try
        {
            await ScanDirectoriesAsync();

            IsLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    public async Task InitializeAsync()
    {
        ValidateDirectories();

        if (IsSelector)
            return;

        await RunScanner();
    }

    private static async Task ScanDirectoriesAsync(CancellationToken ct = default)
    {
        var config = ConfigurationService.LoadConfig();
        
        var validPaths = config.SolutionDirectories.Where(Directory.Exists).ToList();
        if (!validPaths.Any()) return;
        
        await SolutionScanner.ScanAsync(validPaths, config.CSharpSolutionPrefixes, ct);
    }
}