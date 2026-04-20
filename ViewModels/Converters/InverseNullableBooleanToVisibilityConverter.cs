using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Felweed.ViewModels.Converters;

public class InverseNullableBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility is Visibility.Collapsed or Visibility.Hidden;
        }

        return false;
    }
}