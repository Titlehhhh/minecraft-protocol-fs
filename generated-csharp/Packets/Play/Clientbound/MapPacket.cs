using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class MapPacket : IProtocolType<MapPacket>
{
    public int ItemDamage { get; }
    public int Scale { get; }
    public bool Locked { get; }
    public int Columns { get; }
    public MapColorData? ColorData { get; }

    public MapPacket(int itemDamage, int scale, bool locked, int columns, MapColorData? colorData)
    {
        ItemDamage = itemDamage;
        Scale = scale;
        Locked = locked;
        Columns = columns;
        ColorData = colorData;
    }

    public static MapPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<MapPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            var itemDamage = reader.ReadVarInt();
            var scale = reader.ReadSignedByte();
            reader.ReadBoolean();
            var locked = reader.ReadBoolean();
            int skipIconsCount = reader.ReadVarInt();
            for (int i = 0; i < skipIconsCount; i++)
                reader.ReadType<MapIcon>(protocolVersion);
            var columns = reader.ReadSignedByte();
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            return new MapPacket(itemDamage, scale, locked, columns, default!);
        }

        if (protocolVersion >= 765)
        {
            var itemDamage = reader.ReadVarInt();
            var scale = reader.ReadSignedByte();
            var locked = reader.ReadBoolean();
            // TODO(codegen): discard 'icons' (Option: Array (Named "MapIcon", VarIntCount))
            var columns = reader.ReadUnsignedByte();
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            return new MapPacket(itemDamage, scale, locked, columns, default!);
        }

        throw new System.NotSupportedException($"MapPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<MapPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            writer.WriteVarInt(ItemDamage);
            writer.WriteSignedByte((sbyte)Scale);
            writer.WriteBoolean(default);
            writer.WriteBoolean(Locked);
            writer.WriteVarInt(0);
            writer.WriteSignedByte((sbyte)Columns);
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            return;
        }

        if (protocolVersion >= 765)
        {
            writer.WriteVarInt(ItemDamage);
            writer.WriteSignedByte((sbyte)Scale);
            writer.WriteBoolean(Locked);
            writer.WriteBoolean(false);
            writer.WriteUnsignedByte((byte)Columns);
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            return;
        }

        throw new System.NotSupportedException($"MapPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
