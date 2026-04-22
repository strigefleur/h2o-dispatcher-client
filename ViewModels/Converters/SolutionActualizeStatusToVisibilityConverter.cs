using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionActualizeStatusToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SolutionActualizeStatus.Success or SolutionActualizeStatus.Failed or SolutionActualizeStatus.Skipped =>
                Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}