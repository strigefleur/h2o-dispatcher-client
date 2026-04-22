using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionActualizeStatusToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SolutionActualizeStatus.Success => Brushes.LimeGreen,
            SolutionActualizeStatus.Failed => Brushes.Red,
            SolutionActualizeStatus.Skipped => Brushes.Gray,
            _ => Brushes.Orange
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}