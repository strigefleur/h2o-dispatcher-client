using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels.Dialogs;

public partial class NexusCredentialsDialogVm : ObservableObject
{
    [ObservableProperty]
    public partial string? NexusUsername { get; set; }
}