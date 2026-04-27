using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels.Dialogs;

public partial class ActualizerResultDialogVm : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; }
}