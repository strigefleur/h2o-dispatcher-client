using System.Windows.Input;
using Felweed.Models.Enumerators;
using Felweed.Services;
using Felweed.ViewModels;
using Felweed.Views.Dialogs;
using Felweed.Views.MainMenu.About;
using Felweed.Views.MainMenu.Scripts;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Felweed.Views;

public partial class MainWindow
{
    public MainViewModel ViewModel { get; }
    
    private readonly IContentDialogService _contentDialogService;
    private readonly INavigationService _navigationService;
    
    public MainWindow(
        MainViewModel viewModel,
        INavigationViewPageProvider pageProvider, 
        INavigationService navigationService,
        IContentDialogService contentDialogService)
    {
        _contentDialogService = contentDialogService;
        _navigationService = navigationService;
        
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = viewModel;
        
        RootNavigation.SetPageProviderService(pageProvider);
        navigationService.SetNavigationControl(RootNavigation);
        
        contentDialogService.SetDialogHost(RootContentDialogHost);
        
        RootNavigation.Navigating += OnRootNavigationNavigating;
    }

    private async void MainWindow_OnContentRendered(object? sender, EventArgs e)
    {
        await ViewModel.InitializeAsync();
        
        RootNavigation.Navigate(typeof(AboutPage));
    }

    private void BackendGraphPage_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        MainViewModel.GraphPageSelector = SolutionKind.CSharp;
    }

    private void FrontendGraphPage_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        MainViewModel.GraphPageSelector = SolutionKind.Angular;
    }
    
    private async void OnRootNavigationNavigating(NavigationView sender, NavigatingCancelEventArgs args)
    {
        switch (args.Page)
        {
            case BackendDepActualizerPage:
            {
                if (SecureStorage.LoadApiKey() == null)
                {
                    // Stop the navigation
                    args.Cancel = true;

                    // Show prompt
                    if (await ShowGitlabTokenDialogAsync())
                    {
                        _navigationService.Navigate(typeof(BackendDepActualizerPage));
                    }
                }

                break;
            }
            case FrontendDepActualizerPage:
                break;
        }
    }
    
    private async Task<bool> ShowGitlabTokenDialogAsync(CancellationToken ct = default)
    {
        GitlabApiKeyDialog? dialog = null;
    
        try
        {
            dialog = new GitlabApiKeyDialog();
    
            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "API-ключ для Gitlab",
                    Content = dialog,
                    PrimaryButtonText = "Применить",
                    CloseButtonText = "Уже не хочется",
                }, cancellationToken: ct);
    
            if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.ViewModel.GitlabApiKey))
                return false;

            if (SecureStorage.SaveApiKey(dialog.ViewModel.GitlabApiKey))
                return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to use API key");
            return false;
        }
        finally
        {
            dialog?.ViewModel.GitlabApiKey = null;
            dialog?.GitlabApiKeyBox.Text = "";
        }
        
        return false;
    }
}