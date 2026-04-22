using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Services;
using Felweed.Views;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Felweed.ViewModels;

public partial class ScriptPageViewModel(IContentDialogService contentDialogService) : ObservableObject
{
    [ObservableProperty] public partial bool IsInit { get; set; }
    
    public async Task InitAsync(CancellationToken ct = default)
    {
        if (SecureStorage.LoadApiKey() != null)
        {
            IsInit = true;
            return;
        }
        
        GitlabApiKeyDialog? dialog = null;

        try
        {
            dialog = new GitlabApiKeyDialog();

            var result = await contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "API-ключ для Gitlab",
                    Content = dialog,
                    PrimaryButtonText = "Применить",
                    CloseButtonText = "Уже не хочется",
                }, cancellationToken: ct);

            if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.ViewModel.GitlabApiKey))
                return;

            if (SecureStorage.SaveApiKey(dialog.ViewModel.GitlabApiKey))
            {
                IsInit = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to use API key");
        }
        finally
        {
            dialog?.ViewModel.GitlabApiKey = null;
            dialog?.GitlabApiKeyBox.Text = "";
        }
    }
}