using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionKindToPackTooltipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionKind kind)
        {
            return kind switch
            {
                SolutionKind.Angular => "Вызывает команду [yalc publish:local] для зависимости",
                SolutionKind.CSharp => "Вызывает команду pack.cmd для зависимости",
                _ => "???"
            };
        }

        return "???";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}