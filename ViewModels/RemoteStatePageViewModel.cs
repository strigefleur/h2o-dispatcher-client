using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Extensions;
using Felweed.Models.Digestion;
using Felweed.Services;

namespace Felweed.ViewModels;

public partial class RemoteStatePageViewModel : ObservableObject
{
    [ObservableProperty] private bool _isConnecting = true;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private string? _error;
    
    private static HubConnector? _connector;
    private CobwebState? _state;

    public async Task ConnectAsync()
    {
        _connector ??= new HubConnector();
        Error = await _connector.Init(OnFullState);

        IsConnecting = false;
        
        if (Error == null)
        {
            IsConnected = true;
        }
        else
        {
            IsError = true;
        }
    }

    private void OnFullState(byte[] data)
    {
        _state = data.CobwebDecompress<CobwebState>();
    }
}