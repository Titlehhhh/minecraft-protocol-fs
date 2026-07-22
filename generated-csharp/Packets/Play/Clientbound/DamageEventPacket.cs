using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(762, MinecraftVersion.LatestProtocol)]
public sealed partial class DamageEventPacket : IProtocolType<DamageEventPacket>
{
    public int EntityId { get; }
    public int SourceTypeId { get; }
    public int SourceCauseId { get; }
    public int SourceDirectId { get; }
    public Vec3f64? SourcePosition { get; }

    public DamageEventPacket(int entityId, int sourceTypeId, int sourceCauseId, int sourceDirectId, Vec3f64? sourcePosition)
    {
        EntityId = entityId;
        SourceTypeId = sourceTypeId;
        SourceCauseId = sourceCauseId;
        SourceDirectId = sourceDirectId;
        SourcePosition = sourcePosition;
    }

    public static DamageEventPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<DamageEventPacket>(protocolVersion);
        var entityId = reader.ReadVarInt();
        var sourceTypeId = reader.ReadVarInt();
        var sourceCauseId = reader.ReadVarInt();
        var sourceDirectId = reader.ReadVarInt();
        Vec3f64? sourcePosition = null;
        if (reader.ReadBoolean())
            sourcePosition = reader.ReadType<Vec3f64>(protocolVersion);
        return new DamageEventPacket(entityId, sourceTypeId, sourceCauseId, sourceDirectId, sourcePosition);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<DamageEventPacket>(protocolVersion);
        writer.WriteVarInt(EntityId);
        writer.WriteVarInt(SourceTypeId);
        writer.WriteVarInt(SourceCauseId);
        writer.WriteVarInt(SourceDirectId);
        writer.WriteBoolean(SourcePosition is not null);
        if (SourcePosition is { } sourcePositionValue)
            writer.WriteType<Vec3f64>(sourcePositionValue, protocolVersion);
    }
}
