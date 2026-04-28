using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models.AppConfig;
using Felweed.Services;
using Microsoft.Win32;

namespace Felweed.ViewModels.Dialogs;

public partial class AppProfileConfigVm : ObservableObject
{
    [ObservableProperty] public partial string? Name { get; set; }
    [ObservableProperty] public partial string? Description { get; set; }
    
    [ObservableProperty] public partial ObservableCollection<string> SolutionDirectories { get; set; } = [];

    [ObservableProperty] public partial string? CSharpCorporateL0Prefix { get; set; }
    [ObservableProperty] public partial string? CSharpCorporateL1Prefix { get; set; }
    
    [ObservableProperty] public partial string? AngularCorporateL0Prefix { get; set; }
    [ObservableProperty] public partial string? AngularCorporateL1Prefix { get; set; }
    
    [ObservableProperty] public partial string? ActiveBranch { get; set; }
    
    [ObservableProperty] public partial string? DepsGoogleTableUrl { get; set; }
    
    public bool HasChanges { get; private set; }
    
    [RelayCommand]
    private void AddPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        if (dialog.ShowDialog() == true && !SolutionDirectories.Contains(dialog.FolderName) &&
            Directory.Exists(dialog.FolderName))
        {
            HasChanges = true;
            
            SolutionDirectories.Add(dialog.FolderName);
            Save();
        }
    }

    [RelayCommand]
    private void RemovePath(string? path)
    {
        if (path != null)
        {
            HasChanges = true;
            
            SolutionDirectories.Remove(path);
            Save();
        }
    }

    private void Save()
    {
        var profile = new AppProfileConfig
        {
            Name = Name,
            Description = Description,
            CSharpCorporateL0Prefix = CSharpCorporateL0Prefix,
            CSharpCorporateL1Prefix = CSharpCorporateL1Prefix,
            AngularCorporateL0Prefix = AngularCorporateL0Prefix,
            AngularCorporateL1Prefix = AngularCorporateL1Prefix,
            ActiveBranch = ActiveBranch,
            DepsGoogleTableUrl = DepsGoogleTableUrl == null ? null : new Uri(DepsGoogleTableUrl),
            SolutionDirectories = SolutionDirectories.ToList()
        };
        
        ConfigurationService.UpdateProfile(profile);
    }
}