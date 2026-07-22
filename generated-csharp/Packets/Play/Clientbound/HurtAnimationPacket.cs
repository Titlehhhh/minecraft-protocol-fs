using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(762, MinecraftVersion.LatestProtocol)]
public readonly partial record struct HurtAnimationPacket(int EntityId, float Yaw) : IProtocolType<HurtAnimationPacket>
{
    public static HurtAnimationPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<HurtAnimationPacket>(protocolVersion);
        var entityId = reader.ReadVarInt();
        var yaw = reader.ReadFloat();
        return new HurtAnimationPacket(entityId, yaw);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<HurtAnimationPacket>(protocolVersion);
        writer.WriteVarInt(EntityId);
        writer.WriteFloat(Yaw);
    }
}
