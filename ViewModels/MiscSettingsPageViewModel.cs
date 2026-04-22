using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Services;
using Felweed.Views;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Felweed.ViewModels;

public partial class MiscSettingsPageViewModel : ObservableObject
{
    private readonly IContentDialogService _contentDialogService;
    
    [ObservableProperty] private ObservableCollection<MiscSettingVm> _miscSettings = [];
    [ObservableProperty] private bool _canUpdateNexusPassword;

    public MiscSettingsPageViewModel(IContentDialogService contentDialogService)
    {
        _contentDialogService = contentDialogService;
        
        var config = ConfigurationService.LoadConfig();

        MiscSettings.Add(new()
        {
            Name = "Адрес сервера",
            Value = config.ServerUrl,
            AppConfigPropName = nameof(config.ServerUrl),
            Id = 1,
        });

        MiscSettings.Add(new()
        {
            Name = "Каталог конфига Nuget",
            Value = config.NugetConfigPath,
            AppConfigPropName = nameof(config.NugetConfigPath),
            Id = 100,
        });

        MiscSettings.Add(new()
        {
            Name = "Название фида Nuget",
            Value = config.CorporateNexusSourceName,
            AppConfigPropName = nameof(config.CorporateNexusSourceName),
            Id = 101,
        });

        MiscSettings.Add(new()
        {
            Name = "Адрес фида Nuget",
            Value = config.CorporateNexusSourceUrl,
            AppConfigPropName = nameof(config.CorporateNexusSourceUrl),
            Id = 102,
        });

        UpdateNexusUpdateAvailability();
    }

    private void UpdateNexusUpdateAvailability()
    {
        if (string.IsNullOrWhiteSpace(MiscSettings.SingleOrDefault(x => x.Id == 100)?.Value) ||
            string.IsNullOrWhiteSpace(MiscSettings.SingleOrDefault(x => x.Id == 101)?.Value) ||
            UrlHelper.GetSafeUrl(MiscSettings.SingleOrDefault(x => x.Id == 102)?.Value) == null)
        {
            CanUpdateNexusPassword = false;
            return;
        }

        CanUpdateNexusPassword = true;
    }
    
    public void OnSettingVmUpdated(MiscSettingVm vm)
    {
        UpdateNexusUpdateAvailability();
        
        var config = ConfigurationService.LoadConfig();
        config[vm.AppConfigPropName] = vm.Value;
        ConfigurationService.SaveConfig();
    }

    [RelayCommand]
    private async Task OnShowDialog()
    {
        NexusCredentialsDialog? dialog = null;

        try
        {
            dialog = new NexusCredentialsDialog();

            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "Учетная запись для Nexus",
                    Content = dialog,
                    PrimaryButtonText = "Применить",
                    CloseButtonText = "Уже не хочется",
                }
            );

            if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.ViewModel.NexusUsername))
                return;

            var feedConfig = ConfigurationService.ReadNugetFeedConfig();
            if (!feedConfig.IsValid())
                return;

            NugetHelper.SetNugetCredentials(feedConfig.ConfigPath, feedConfig.Name, feedConfig.Url,
                dialog.ViewModel.NexusUsername, dialog.NexusPasswordBox.Password);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply nuget credentials");
        }
        finally
        {
            dialog?.ViewModel.NexusUsername = null;
            dialog?.NexusPasswordBox.Password = "";
        }
    }
}