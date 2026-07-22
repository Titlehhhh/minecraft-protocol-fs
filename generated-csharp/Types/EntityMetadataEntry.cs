using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(766, MinecraftVersion.LatestProtocol)]
public sealed partial class EntityMetadataEntry : IProtocolType<EntityMetadataEntry>
{
    public int Index { get; }
    public EntityMetadataValue Value { get; }

    public EntityMetadataEntry(int index, EntityMetadataValue value)
    {
        Index = index;
        Value = value;
    }

    public static EntityMetadataEntry Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityMetadataEntry>(protocolVersion);
        var index = reader.ReadUnsignedByte();
        var _type = reader.ReadVarInt();
        // TODO(codegen): ReadUnion ("_type", "EntityMetadataValue", "Value")
        return new EntityMetadataEntry(index, default!);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<EntityMetadataEntry>(protocolVersion);
        writer.WriteUnsignedByte((byte)Index);
    // TODO(codegen): write wire-only '_type' (derive from model)
    // TODO(codegen): ReadUnion ("_type", "EntityMetadataValue", "Value")
    }
}
