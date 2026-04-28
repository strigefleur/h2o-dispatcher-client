using Felweed.Constants;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Felweed.Services;

public static class HubConnector
{
    public static HubConnection? Connection { get; private set; }
    
    public static event Action<HubConnectionState>? StateChanged;

    public static async Task<string?> InitAsync(Action<byte[]> onFullState)
    {
        var config = ConfigurationService.LoadConfig();
        var hubUrl = config.ActiveProfile.GetHubUrl();

        if (hubUrl == null)
            return "Отсутствует адрес подключения";

        try
        {
            if (Connection == null)
            {
                Connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, o =>
                    {
                        o.Transports = HttpTransportType.WebSockets;
                    })
                    .AddMessagePackProtocol(options =>
                    {
                        var resolver = CompositeResolver.Create(
                            DynamicEnumAsStringResolver.Instance,
                            ContractlessStandardResolver.Instance
                        );
                        options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
                    })
                    .WithAutomaticReconnect()
                    .Build();
                
                Connection.On(SignalrConst.Events.OnFullState, onFullState);
                
                Connection.Reconnecting += (e) => Notify(HubConnectionState.Reconnecting);
                Connection.Reconnected += (id) => Notify(HubConnectionState.Connected);
                Connection.Closed += (e) => Notify(HubConnectionState.Disconnected);
            }

            if (Connection.State == HubConnectionState.Disconnected)
            {
                await Connection.StartAsync();
                await Notify(HubConnectionState.Connected);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize connection");
            return ex.Message;
        }

        return null;
    }
    
    private static Task Notify(HubConnectionState state)
    {
        StateChanged?.Invoke(state);
        return Task.CompletedTask;
    }
    
    public static async Task CleanupConnectionAsync()
    {
        if (Connection != null)
        {
            try
            {
                // Explicitly stop the network traffic first
                await Connection.StopAsync(); 
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error cleaning up connection");
            }
            finally
            {
                await Connection.DisposeAsync();
            }
        }
    }
}