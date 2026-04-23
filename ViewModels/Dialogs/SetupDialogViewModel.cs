using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Felweed.ViewModels.Dialogs;

public partial class SetupDialogViewModel : ObservableObject
{
    public ObservableCollection<string> SelectedPaths { get; } = [];

    [RelayCommand]
    private void AddPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true && !SelectedPaths.Contains(dialog.FolderName))
        {
            SelectedPaths.Add(dialog.FolderName);
        }
    }

    [RelayCommand]
    private void RemovePath(string? path)
    {
        if (path != null)
            SelectedPaths.Remove(path);
    }
}