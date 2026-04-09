using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionKindToWatermarkTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionKind kind)
        {
            return kind switch
            {
                SolutionKind.Angular => "TS",
                SolutionKind.CSharp => "C#",
                _ => "???"
            };
        }

        return "???";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}