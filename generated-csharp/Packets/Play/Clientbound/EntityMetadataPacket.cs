using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class EntityMetadataPacket : IProtocolType<EntityMetadataPacket>
{
    public int EntityId { get; }
    public EntityMetadataEntry[] Metadata { get; }

    public EntityMetadataPacket(int entityId, EntityMetadataEntry[] metadata)
    {
        EntityId = entityId;
        Metadata = metadata;
    }

    public static EntityMetadataPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityMetadataPacket>(protocolVersion);
        var entityId = reader.ReadVarInt();
        // TODO(codegen): read 'Metadata' (SentinelArray (Named "EntityMetadataEntry", 255))
        return new EntityMetadataPacket(entityId, default!);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityMetadataPacket>(protocolVersion);
        writer.WriteVarInt(EntityId);
    // TODO(codegen): write 'Metadata' (SentinelArray (Named "EntityMetadataEntry", 255))
    }
}
