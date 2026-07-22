using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct TeleportConfirmPacket(int TeleportId) : IProtocolType<TeleportConfirmPacket>
{
    public static TeleportConfirmPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<TeleportConfirmPacket>(protocolVersion);
        var teleportId = reader.ReadVarInt();
        return new TeleportConfirmPacket(teleportId);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<TeleportConfirmPacket>(protocolVersion);
        writer.WriteVarInt(TeleportId);
    }
}
