using System.Diagnostics;
using System.IO;
using System.Windows;
using CliWrap;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Extensions;
using Felweed.Models.Enumerators;
using Felweed.Services;
using Felweed.Views.Dialogs;
using Microsoft.AspNetCore.SignalR.Client;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Felweed.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IContentDialogService _contentDialogService;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSelector { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }
    
    [ObservableProperty]
    public partial string ActiveProfileName { get; set; }
    
    [ObservableProperty]
    public partial Uri? DepsGoogleTableUrl { get; set; }

    [ObservableProperty]
    public partial SymbolRegular ThemeSwitchIcon { get; set; } =
        ConfigurationService.LoadConfig().CurrentProfile == null
            ? ApplicationThemeExtensions.LightThemeSymbol
            : ConfigurationService.LoadConfig().ActiveProfile.Theme.GetThemeSymbol();

    [ObservableProperty] public partial HubConnectionState ConnectionState { get; set; }
    
    public static SolutionKind GraphPageSelector { get; set; } = SolutionKind.CSharp;

    public MainViewModel(IContentDialogService contentDialogService)
    {
        _contentDialogService =  contentDialogService;
        
        ConnectionState = HubConnector.Connection?.State ?? HubConnectionState.Disconnected;
        HubConnector.StateChanged += OnSignalRStateChanged;
        
        var config = ConfigurationService.LoadConfig();
        ActiveProfileName = config.CurrentProfileName ?? "N/A";
        DepsGoogleTableUrl = config.CurrentProfile?.DepsGoogleTableUrl;
    }
    
    private void OnSignalRStateChanged(HubConnectionState newState)
    {
        App.Current.Dispatcher.Invoke(() => ConnectionState = newState);
    }
    
    [RelayCommand]
    private async Task ConfirmSelector()
    {
        await OpenProfileSelectorAsync();
    }
    
    [RelayCommand]
    private void SwitchTheme()
    {
        ThemeSwitchIcon = ThemeManager.SwitchTheme();
        ConfigurationService.SetThemeBySymbol(ThemeSwitchIcon);
    }

    [RelayCommand]
    private async Task OpenProfileSelector()
    {
        await OpenProfileSelectorAsync();
    }
    
    [RelayCommand]
    private async Task OpenLogDir()
    {
        await Cli.Wrap("explorer.exe")
            .WithArguments(LogHelper.LogDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
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

    private async Task OpenProfileSelectorAsync()
    {
        var config = ConfigurationService.LoadConfig();
        var activeProfileBefore = config.CurrentProfileName;

        var dialog = new ProfileSelectorDialog();
        var result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions()
            {
                Title = "Выбор профиля",
                Content = dialog,
                PrimaryButtonText = "Ок",
                CloseButtonText = "Ну, ок"
            }
        );

        // if (result == ContentDialogResult.Primary)
        {
            var selectedProfileName = dialog.ViewModel.SelectedProfile?.Name;
            var profileChanged = selectedProfileName != activeProfileBefore;
            if (profileChanged)
            {
                ConfigurationService.SetActiveProfile(selectedProfileName);
            }

            if (dialog.ViewModel.HasChanges || profileChanged)
            {
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Application.Current.Shutdown();
            }
        }
    }

    public async Task InitializeAsync()
    {
        var config = ConfigurationService.LoadConfig();
        if (config.CurrentProfile == null || !config.ActiveProfile.Validate())
        {
            await OpenProfileSelectorAsync();
        }

        await RunScanner();
    }

    private static async Task ScanDirectoriesAsync(CancellationToken ct = default)
    {
        var config = ConfigurationService.LoadConfig();
        
        var validPaths = config.ActiveProfile.SolutionDirectories.Where(Directory.Exists).ToList();
        if (!validPaths.Any()) return;
        
        await SolutionScanner.ScanAsync(validPaths, [config.ActiveProfile.CSharpCorporateL1Prefix], ct);
    }
}