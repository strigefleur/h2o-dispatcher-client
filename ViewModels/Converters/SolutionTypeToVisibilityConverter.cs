using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionType type)
        {
            return type switch
            {
                SolutionType.Library => Visibility.Collapsed,
                SolutionType.Service => Visibility.Visible,
                _ => throw new InvalidOperationException()
            };
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}