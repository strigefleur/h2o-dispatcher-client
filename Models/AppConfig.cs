using Felweed.Constants;
using Wpf.Ui.Controls;

namespace Felweed.Models;

public sealed record AppConfig
{
    public List<string> SolutionDirectories { get; set; } = [];
    public List<string> CSharpSolutionPrefixes { get; set; } = [Constants.PrefixConst.CSharpCorporateL2Prefix, "DataHub"];
    public SymbolRegular ThemeSwitchIcon { get; set; } = SymbolRegular.WeatherMoon24;

    public List<EnvVariable> EnvVariables { get; set; } = [..EnvVariableConst.DefaultEnvVariables];
}