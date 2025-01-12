using System.Collections.Frozen;
using System.Collections.Immutable;

namespace SandBoxLib;

public partial class PacketFactory
{
    private static readonly FrozenDictionary<long, Func<IServerPacket>> ClientboundPackets = new Dictionary<long,Func<IServerPacket>>
                               {
                                   {6, () => new TestPacket()},
                               }.ToFrozenDictionary();
}

internal class TestPacket : IServerPacket
{
    public void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        throw new NotImplementedException();
    }
}