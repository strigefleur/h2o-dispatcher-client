using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;

namespace Felweed.ViewModels.Converters;

public class ConnectionStateToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HubConnectionState state)
        {
            switch (state)
            {
                case HubConnectionState.Disconnected:
                    return Brushes.Red;
                case HubConnectionState.Connected:
                    return Brushes.Green;
                case HubConnectionState.Connecting:
                case HubConnectionState.Reconnecting:
                    return Brushes.Orange;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        return Brushes.BlueViolet;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}