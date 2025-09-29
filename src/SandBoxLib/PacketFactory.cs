using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SandBoxLib;

public static partial class PacketFactory
{
  
    public static IServerPacket CreateClientboundPacket(int protocolVersion, int packetId)
    {
        return ClientboundPackets[Combine(protocolVersion, packetId)]();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Combine(int a, int b)
    {
        return (long)a << 32 | (uint)b;
    }
}