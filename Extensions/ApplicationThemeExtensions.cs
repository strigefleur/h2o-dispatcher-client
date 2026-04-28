using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Felweed.Extensions;

public static class ApplicationThemeExtensions
{
    public const SymbolRegular LightThemeSymbol = SymbolRegular.WeatherMoon24;
    public const SymbolRegular DarkThemeSymbol = SymbolRegular.WeatherSunny24;

    public static SymbolRegular GetThemeSymbol(this ApplicationTheme theme) => theme == ApplicationTheme.Light
        ? LightThemeSymbol
        : DarkThemeSymbol;
    
    public static ApplicationTheme GetTheme(this SymbolRegular symbol) => symbol == LightThemeSymbol
        ? ApplicationTheme.Dark
        : ApplicationTheme.Light;
}