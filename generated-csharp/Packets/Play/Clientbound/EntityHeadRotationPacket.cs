using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct EntityHeadRotationPacket(int EntityId, int HeadYaw) : IProtocolType<EntityHeadRotationPacket>
{
    public static EntityHeadRotationPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityHeadRotationPacket>(protocolVersion);
        var entityId = reader.ReadVarInt();
        var headYaw = reader.ReadSignedByte();
        return new EntityHeadRotationPacket(entityId, headYaw);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityHeadRotationPacket>(protocolVersion);
        writer.WriteVarInt(EntityId);
        writer.WriteSignedByte((sbyte)HeadYaw);
    }
}
