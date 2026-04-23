using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels.Dialogs;

public partial class GitlabApiKeyDialogVm : ObservableObject
{
    [ObservableProperty]
    public partial string? GitlabApiKey { get; set; }
}