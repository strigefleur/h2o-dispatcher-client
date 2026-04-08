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

    private async Task<HubConnection?> Init()
    {
        var config = ConfigurationService.LoadConfig();
        var hubUrl = config.GetHubUrl();
        
        if (hubUrl != null)
        {
            _connection ??= new HubConnectionBuilder()
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
            
            _connection.On<byte[]>(SignalrConst.Events.OnFullState, data =>
            {
                var state = data.CobwebDecompress<CobwebState>();
            });
            
            await _connection.InvokeAsync(SignalrConst.Methods.Subscribe);
            
            await _connection.StartAsync();
        }
        
        return _connection;
    }
}