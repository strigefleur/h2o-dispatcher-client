using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels;

public partial class NexusCredentialsDialogVm : ObservableObject
{
    [ObservableProperty]
    public partial string? NexusUsername { get; set; }
}