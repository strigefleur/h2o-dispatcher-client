using Wpf.Ui.Controls;

namespace Felweed.Models;

public sealed record AppConfig
{
    public List<string> SolutionDirectories { get; set; } = [];
    public List<string> CSharpSolutionPrefixes { get; set; } = ["CFO", "DataHub"];
    public SymbolRegular ThemeSwitchIcon { get; set; } = SymbolRegular.WeatherMoon24;
}