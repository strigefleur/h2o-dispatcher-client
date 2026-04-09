using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Felweed.ViewModels.Converters;

public class SolutionOutdatedToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)values[0] ? Brushes.Red : values[1] as Brush ?? Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}