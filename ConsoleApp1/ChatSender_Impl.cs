using System.Collections.Frozen;

namespace ConsoleApp1;

public partial struct UseItemSender
{
    public ValueTask Send(int hand)
    {
        using var writer = new MinecraftPrimitiveWriter();
        int packetId = PacketIdHelper.GetPacketId(_currentProtocolVersion, ClientPacket.UseItem);
        writer.WriteVarInt(packetId);
        
        
        
        return _packetSender.SendPacket(writer.GetWrittenMemory());
    }
    
    
    
    
}

public static class PacketIdHelper
{
    // client packet id maps
    private static readonly FrozenDictionary<long, int> ClientPacketIdMap;

    public static int GetPacketId(int protocol, ClientPacket packet)
    {
        int packetAsInt = (int)packet;
        return ClientPacketIdMap[Combine(protocol, packetAsInt)];
    }

    private static long Combine(int a, int b)
    {
        //Первые 4 байта a,вторые 4 байта b
        return (long)a << 32 | (uint)b;
    }
}