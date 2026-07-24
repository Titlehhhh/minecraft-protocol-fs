using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Handshaking.Serverbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct LegacyServerListPingPacket(int Payload) : IProtocolType<LegacyServerListPingPacket>
{
    public static LegacyServerListPingPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LegacyServerListPingPacket>(protocolVersion);
        var payload = reader.ReadUnsignedByte();
        return new LegacyServerListPingPacket(payload);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LegacyServerListPingPacket>(protocolVersion);
        writer.WriteUnsignedByte((byte)Payload);
    }
}
