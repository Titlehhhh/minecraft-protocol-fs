using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct PingRequestPacket(long Time) : IProtocolType<PingRequestPacket>
{
    public static PingRequestPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PingRequestPacket>(protocolVersion);
        var time = reader.ReadSignedLong();
        return new PingRequestPacket(time);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PingRequestPacket>(protocolVersion);
        writer.WriteSignedLong(Time);
    }
}
