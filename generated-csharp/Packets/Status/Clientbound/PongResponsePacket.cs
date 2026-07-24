using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Status.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct PongResponsePacket(long Time) : IProtocolType<PongResponsePacket>
{
    public static PongResponsePacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PongResponsePacket>(protocolVersion);
        var time = reader.ReadSignedLong();
        return new PongResponsePacket(time);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PongResponsePacket>(protocolVersion);
        writer.WriteSignedLong(Time);
    }
}
