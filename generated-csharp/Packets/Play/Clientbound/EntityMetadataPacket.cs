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

    public static int GetPacketId(int protocolVersion)
    {
        if (protocolVersion >= 735 && protocolVersion <= 736)
            return 0x44;
        if (protocolVersion >= 751 && protocolVersion <= 754)
            return 0x44;
        if (protocolVersion >= 755 && protocolVersion <= 755)
            return 0x4D;
        if (protocolVersion >= 756 && protocolVersion <= 756)
            return 0x4D;
        if (protocolVersion >= 757 && protocolVersion <= 758)
            return 0x4D;
        if (protocolVersion >= 759 && protocolVersion <= 759)
            return 0x4D;
        if (protocolVersion >= 760 && protocolVersion <= 760)
            return 0x50;
        if (protocolVersion >= 761 && protocolVersion <= 761)
            return 0x4E;
        if (protocolVersion >= 762 && protocolVersion <= 763)
            return 0x52;
        if (protocolVersion >= 764 && protocolVersion <= 764)
            return 0x54;
        if (protocolVersion >= 765 && protocolVersion <= 765)
            return 0x56;
        if (protocolVersion >= 766 && protocolVersion <= 766)
            return 0x58;
        if (protocolVersion >= 767 && protocolVersion <= 767)
            return 0x58;
        if (protocolVersion >= 768 && protocolVersion <= 769)
            return 0x5D;
        if (protocolVersion >= 770 && protocolVersion <= 770)
            return 0x5C;
        if (protocolVersion >= 771 && protocolVersion <= 772)
            return 0x5C;
        throw new System.NotSupportedException($"No packet id for protocol {protocolVersion}.");
    }
}
