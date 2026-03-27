using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels.Converters;

public class DependencyTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionDependencyType type)
        {
            return type switch
            {
                SolutionDependencyType.Public => SymbolRegular.Globe24,
                SolutionDependencyType.Corporate => SymbolRegular.Building24,
                _ => SymbolRegular.Globe24
            };
        }

        return SymbolRegular.Question24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}