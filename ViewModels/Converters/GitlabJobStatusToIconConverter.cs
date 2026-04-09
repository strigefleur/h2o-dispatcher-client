using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class GitlabJobStatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is JobStatus status ? status switch
        {
            JobStatus.Success => "/Assets/Icons/Gitlab/mr-status-success.svg",
            JobStatus.Failed => "/Assets/Icons/Gitlab/mr-status-error.svg",
            JobStatus.Running => "/Assets/Icons/Gitlab/mr-status-low.svg",
            JobStatus.Skipped => "/Assets/Icons/Gitlab/status_skipped.svg",
            _ => "/Assets/Icons/Gitlab/mr-status-unknown.svg"
        } : null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}