using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Constants;
using Felweed.Models;
using Felweed.Services;

namespace Felweed.ViewModels.MainMenu.Settings.EnvVariables;

public partial class EnvVariablesPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<EnvVariableVm> EnvVariables { get; set; } = [];

    public EnvVariablesPageViewModel()
    {
        var config = ConfigurationService.LoadConfig();

        foreach (var envVariable in config.EnvVariables)
        {
            EnvVariables.Add(new()
            {
                Name = envVariable.Name,
                Value = envVariable.Value
            });
        }
    }
    
    [RelayCommand]
    private void RevertEnvToDefault(EnvVariableVm envVariable)
    {
        var defaultEnvVariable = EnvVariableConst.DefaultEnvVariables.Find(x => x.Name == envVariable.Name);
        if (defaultEnvVariable != null)
        {
            envVariable.Value = defaultEnvVariable.Value;
            
            var config = ConfigurationService.LoadConfig();
        
            config.EnvVariables.Clear();
            config.EnvVariables.AddRange(EnvVariables.Select<EnvVariableVm, EnvVariable>(x => new EnvVariable(x.Name, x.Value)));
        
            ConfigurationService.SaveConfig();
        }
    }
}