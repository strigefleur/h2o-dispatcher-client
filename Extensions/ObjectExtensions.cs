using MessagePack;
using MessagePack.Resolvers;

namespace Felweed.Extensions;

public static class ObjectExtensions
{
    public static T CobwebDecompress<T>(this byte[] data, CancellationToken ct = default) =>
        MessagePackSerializer.Deserialize<T>(data, ContractlessStandardResolver.Options, ct);
}