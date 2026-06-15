using System.Threading;
using System.Threading.Tasks;
using PacketGenerator.Protocol.Repository;
using ProtoCore;

namespace PacketGenerator.Protocol.Loading;

public sealed record ProtocolDataOptions(int FromProtocol = 735, int ToProtocol = 772);

public static class ProtocolDataLoader
{
    public static async Task<IProtocolRepository> LoadRepositoryAsync(
        ProtocolDataOptions options,
        CancellationToken cancellationToken = default)
    {
        var protocols = await ProtocolLoader.LoadProtocolsAsync(options.FromProtocol, options.ToProtocol);
        cancellationToken.ThrowIfCancellationRequested();

        var histories = HistoryBuilder.Build(protocols);
        return new ProtocolRepository(
            new ProtocolRange(options.FromProtocol, options.ToProtocol),
            protocols,
            histories);
    }
}
