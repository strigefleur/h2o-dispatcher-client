using System.Windows.Data;
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
                if (!ConfigurationService.IsCredentialsValid())
                {
                    // Stop the navigation
                    args.Cancel = true;

                    // Show prompt
                    if (await ShowCredentialsDialogAsync())
                    {
                        _navigationService.Navigate(typeof(BackendDepActualizerPage));
                    }
                }

                break;
            }
            case BatchRepoCheckoutPage:
            {
                if (!ConfigurationService.IsCredentialsValid())
                {
                    // Stop the navigation
                    args.Cancel = true;

                    // Show prompt
                    if (await ShowCredentialsDialogAsync())
                    {
                        _navigationService.Navigate(typeof(BatchRepoCheckoutPage));
                    }
                }

                break;
            }
            case FrontendDepActualizerPage:
            {
                if (!ConfigurationService.IsCredentialsValid())
                {
                    // Stop the navigation
                    args.Cancel = true;

                    // Show prompt
                    if (await ShowCredentialsDialogAsync())
                    {
                        _navigationService.Navigate(typeof(FrontendDepActualizerPage));
                    }
                }

                break;
            }
        }
    }
    
    private async Task<bool> ShowCredentialsDialogAsync(CancellationToken ct = default)
    {
        CredentialsDialog? credentialsDialog = null;
    
        try
        {
            credentialsDialog = new CredentialsDialog();
            
            var dialog = new ContentDialog(_contentDialogService.GetDialogHostEx())
            {
                Title = "Конфигурация доступа к внешним ресурсам",
                Content = credentialsDialog,
                PrimaryButtonText = "Применить",
                CloseButtonText = "Уже не хочется",
                IsPrimaryButtonEnabled = false
            };

            var vm = credentialsDialog.ViewModel;
            
            BindingOperations.SetBinding(dialog, 
                ContentDialog.IsPrimaryButtonEnabledProperty, 
                new Binding("IsValid") { Source = vm });
            
            credentialsDialog.NexusPasswordBox.PasswordChanged += (s, e) => 
            {
                vm.UpdatePassword(credentialsDialog.NexusPasswordBox.Password);
            };

            var result = await dialog.ShowAsync(ct);
    
            if (result != ContentDialogResult.Primary)
                return false;
            
            var config = ConfigurationService.LoadConfig();
            config.CorporateNexusSourceName = vm.NexusSourceName;
            config.CorporateNexusSourceUrl = vm.NexusSourceUrl;
            ConfigurationService.SaveConfig();

            NugetHelper.SetNugetCredentials(vm.NexusSourceName, vm.NexusSourceUrl,
                vm.NexusUsername, credentialsDialog.NexusPasswordBox.Password);

            if (!SecureStorage.SaveApiKey(vm.GitlabApiKey))
                return false;

            if (!ConfigurationService.IsCredentialsValid())
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set credentials");
            return false;
        }
        finally
        {
            credentialsDialog?.ViewModel.GitlabApiKey = null;
            credentialsDialog?.GitlabApiKeyBox.Text = "";
            
            credentialsDialog?.ViewModel.NexusUsername = null;
            credentialsDialog?.NexusPasswordBox.Password = "";
        }
    }
}