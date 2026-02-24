using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels.Converters;

public class KindToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolutionKind kind)
        {
            return kind switch
            {
                SolutionKind.Angular => SymbolRegular.Globe24,
                SolutionKind.CSharp => SymbolRegular.Desktop24,
                _ => SymbolRegular.Box24
            };
        }

        return SymbolRegular.Question24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}