using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels.Converters;

public class DependencyTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCorporate)
        {
            return isCorporate switch
            {
                false => SymbolRegular.Globe24,
                true => SymbolRegular.Building24,
            };
        }

        return SymbolRegular.Question24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}