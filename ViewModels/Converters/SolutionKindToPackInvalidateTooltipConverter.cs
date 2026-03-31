using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class SolutionKindToPackInvalidateTooltipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionKind kind)
        {
            return kind switch
            {
                SolutionKind.Angular => "Пока ничего не делает :)",
                SolutionKind.CSharp => "Вызывает удаление локального кэша Nuget для зависимости",
                _ => "???"
            };
        }

        return "???";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}