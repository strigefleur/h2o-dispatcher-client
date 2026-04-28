using Felweed.Extensions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Felweed.Services;

public static class ThemeManager
{
    public static SymbolRegular SwitchTheme()
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();

        if (currentTheme == ApplicationTheme.Dark)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            return ApplicationThemeExtensions.LightThemeSymbol;
        }

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
        return ApplicationThemeExtensions.DarkThemeSymbol;
    }
}