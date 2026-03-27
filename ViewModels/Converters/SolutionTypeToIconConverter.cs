using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels.Converters;

public class SolutionTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionType type)
        {
            return type switch
            {
                SolutionType.Service => SymbolRegular.Settings24,
                SolutionType.Library => SymbolRegular.Library24,
                _ => SymbolRegular.Question24
            };
        }

        return SymbolRegular.Question24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}