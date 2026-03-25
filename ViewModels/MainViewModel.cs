using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;

    public SetupDialogViewModel SetupDialogViewModel { get; } = new();
    
    [ObservableProperty]
    private ObservableCollection<Solution> _solutions = [];
    
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSelector;
    [ObservableProperty] private bool _isLoaded;
    
    [ObservableProperty] private SymbolRegular _themeSwitchIcon = SymbolRegular.WeatherMoon24;
    
    [Obsolete("DesignTime Only")]
    public MainViewModel() {}

    public MainViewModel(ConfigurationService configurationService)
    {
        _configService = configurationService;
    }

    [RelayCommand]
    private void RunSolution(Solution? solution)
    {
        var config = _configService.LoadConfig();
        
        solution?.Run([..config.CSharpSolutionPrefixes]);
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
        var config = _configService.LoadConfig();
        
        if (!_configService.ValidateDirectories(config))
        {
            IsSelector = true;
            
            var selectedDirs = SetupDialogViewModel.SelectedPaths.ToList();
            if (selectedDirs.Count == 0)
            {
                return;
            }

            // Save new config
            config.SolutionDirectories = selectedDirs.ToList();
            _configService.SaveConfig(config);
        }

        IsSelector = false;
    }

    private async Task RunScanner()
    {
        IsLoading = true;
        
        try
        {
            var config = _configService.LoadConfig();
            await ScanDirectoriesAsync(config.SolutionDirectories);

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

    private async Task ScanDirectoriesAsync(List<string> paths, CancellationToken ct = default)
    {
        var validPaths = paths.Where(Directory.Exists).ToList();
        if (!validPaths.Any()) return;
        
        var config = _configService.LoadConfig();
        
        var scanner = await SolutionScanner.ScanAsync(validPaths, config.CSharpSolutionPrefixes, ct);

        List<Solution> solutions = [..scanner.CsharpSolutions, ..scanner.AngularSolutions];

        Solutions = new ObservableCollection<Solution>(solutions.OrderByDescending(x => x.Type));
    }
}