using Felweed.Constants;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Felweed.Services;

public class HubConnector
{
    private static HubConnection? _connection;

    public async Task<string?> Init(Action<byte[]> onFullState)
    {
        var config = ConfigurationService.LoadConfig();
        var hubUrl = config.GetHubUrl();

        if (hubUrl == null)
            return "Отсутствует адрес подключения";

        try
        {
            if (_connection == null)
            {
                _connection = new HubConnectionBuilder()
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
                
                _connection.On(SignalrConst.Events.OnFullState, onFullState);
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
                await _connection.InvokeAsync(SignalrConst.Methods.Subscribe);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }

        return null;
    }
    
    public async Task CleanupConnection()
    {
        if (_connection != null)
        {
            try
            {
                // Explicitly stop the network traffic first
                await _connection.StopAsync(); 
            }
            catch (Exception ex)
            {
                // Log issues like network timeout during closure
            }
            finally
            {
                // Always dispose to free up the object's resources
                await _connection.DisposeAsync();
            }
        }
    }

}