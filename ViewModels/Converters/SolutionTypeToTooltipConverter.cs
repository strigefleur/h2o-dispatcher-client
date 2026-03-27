using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionTypeToTooltipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionType type)
        {
            return type switch
            {
                SolutionType.Service => "Сервис",
                SolutionType.Library => "Библиотека",
                _ => "???"
            };
        }

        return "???";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}