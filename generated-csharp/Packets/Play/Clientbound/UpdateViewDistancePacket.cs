using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct UpdateViewDistancePacket(int ViewDistance) : IProtocolType<UpdateViewDistancePacket>
{
    public static UpdateViewDistancePacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateViewDistancePacket>(protocolVersion);
        var viewDistance = reader.ReadVarInt();
        return new UpdateViewDistancePacket(viewDistance);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateViewDistancePacket>(protocolVersion);
        writer.WriteVarInt(ViewDistance);
    }
}
