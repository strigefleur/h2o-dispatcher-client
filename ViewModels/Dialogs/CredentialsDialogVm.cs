using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Services;

namespace Felweed.ViewModels.Dialogs;

public partial class CredentialsDialogVm : ObservableValidator
{
    [ObservableProperty]
    [Required]
    [MinLength(10, ErrorMessage = ":')")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string? GitlabApiKey { get; set; }
    
    [ObservableProperty]
    [Required]
    [MinLength(10, ErrorMessage = ":')")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string? NexusUsername { get; set; }
    
    [ObservableProperty]
    [Required]
    [MinLength(10, ErrorMessage = ":')")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string? NexusSourceName { get; set; }
    
    [ObservableProperty]
    [Required]
    [MinLength(10, ErrorMessage = ":')")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string? NexusSourceUrl { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial bool PasswordControlIsValid { get; set; }
    
    public bool IsValid => !HasErrors && PasswordControlIsValid;

    public CredentialsDialogVm()
    {
        var config = ConfigurationService.LoadConfig();
        
        if (config.CorporateNexusSourceName is not null)
            NexusSourceName = config.CorporateNexusSourceName;
        
        if (config.CorporateNexusSourceUrl is not null)
            NexusSourceUrl = config.CorporateNexusSourceUrl;
        
        ValidateAllProperties();
        OnPropertyChanged(nameof(IsValid));
    }
    
    public void UpdatePassword(string? newPassword)
    {
        PasswordControlIsValid = newPassword?.Length > 10;
        OnPropertyChanged(nameof(IsValid));
    }
}