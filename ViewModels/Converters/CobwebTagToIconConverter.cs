using System.Globalization;
using System.Windows.Data;
using Felweed.Models.Digestion;
using Felweed.Models.Enumerators;

namespace Felweed.ViewModels.Converters;

public class CobwebTagToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CobwebTag tag)
        {
            switch (tag)
            {
                case { LatestPublishStatus: JobStatus.Failed, AllowFailure: true }:
                    return "/Assets/Icons/Gitlab/warning.svg";
                case { LatestPublishStatus: JobStatus.Success }:
                    return "/Assets/Icons/Gitlab/success.svg";
                case { LatestPublishStatus: JobStatus.Failed }:
                    return "/Assets/Icons/Gitlab/failed.svg";
                case { LatestPublishStatus: JobStatus.Running }:
                    return "/Assets/Icons/Gitlab/running.svg";
                case { LatestPublishStatus: JobStatus.Skipped }:
                    return "/Assets/Icons/Gitlab/skipped.svg";
                case { LatestPublishStatus: JobStatus.Canceled }:
                    return "/Assets/Icons/Gitlab/canceled.svg";
            }
        }

        return "/Assets/Icons/Gitlab/unknown.svg";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}