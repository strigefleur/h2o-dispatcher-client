using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Services;

namespace Felweed.ViewModels.Dialogs;

public partial class ProfileSelectorDialogVm : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<AppProfileConfigVm> Profiles { get; set; }
    [ObservableProperty] public partial AppProfileConfigVm? SelectedProfile { get; set; }
    
    public bool HasChanges => Profiles.Any(x => x.HasChanges);

    public ProfileSelectorDialogVm()
    {
        var config = ConfigurationService.LoadConfig();

        Profiles = [];
        foreach (var profileKvp in config.Profiles)
        {
            var profile = profileKvp.Value;
            var vm = new AppProfileConfigVm
            {
                Name = profile.Name,
                Description = profile.Description,
                CSharpCorporateL0Prefix = profile.CSharpCorporateL0Prefix,
                CSharpCorporateL1Prefix = profile.CSharpCorporateL1Prefix,
                AngularCorporateL0Prefix = profile.AngularCorporateL0Prefix,
                AngularCorporateL1Prefix = profile.AngularCorporateL1Prefix,
                ActiveBranch = profile.ActiveBranch,
                DepsGoogleTableUrl = profile.DepsGoogleTableUrl?.AbsoluteUri,
            };

            foreach (var dir in profile.SolutionDirectories)
            {
                vm.SolutionDirectories.Add(dir);
            }
            
            Profiles.Add(vm);
        }

        SelectedProfile = config.CurrentProfileName == null
            ? null
            : Profiles.FirstOrDefault(x => x.Name == config.CurrentProfileName);
    }
}