using Felweed.Constants;
using Felweed.Extensions;
using Felweed.Models.Digestion;
using MessagePack;
using MessagePack.Resolvers;
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
                    .WithUrl(hubUrl)
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
                _connection.On<byte[]>(SignalrConst.Events.OnFullState, data =>
                {
                    var state = data.CobwebDecompress<CobwebState>();
                });
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
}