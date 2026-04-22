using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;
using Wpf.Ui.Controls;

namespace Felweed.ViewModels.Converters;

public class SolutionActualizeStatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SolutionActualizeStatus.Success => SymbolRegular.BracesCheckmark16,
            SolutionActualizeStatus.Failed => SymbolRegular.BracesDismiss16,
            SolutionActualizeStatus.Skipped => SymbolRegular.SkipForwardTab24,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}