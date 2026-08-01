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
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            throw new System.NotImplementedException("TODO(codegen): MapPacket wire layout is not fully generated for this protocol version.");
        }

        if (protocolVersion >= 765)
        {
            // TODO(codegen): discard 'icons' (Option: Array (Named "MapIcon", VarIntCount))
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            throw new System.NotImplementedException("TODO(codegen): MapPacket wire layout is not fully generated for this protocol version.");
        }

        throw new System.NotSupportedException($"MapPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<MapPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            throw new System.NotImplementedException("TODO(codegen): MapPacket wire layout is not fully generated for this protocol version.");
        }

        if (protocolVersion >= 765)
        {
            // TODO(codegen): IfNonZero   ("columns",    [ReadBlock       (Named "MapColorData", "ColorData",        [Read ("rows", I8, "Rows"); Read ("x", I8, "X"); Read ("y", I8, "Y");         Read ("data", ByteArray, "Data")])])
            throw new System.NotImplementedException("TODO(codegen): MapPacket wire layout is not fully generated for this protocol version.");
        }

        throw new System.NotSupportedException($"MapPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public static int GetPacketId(int protocolVersion)
    {
        if (protocolVersion >= 735 && protocolVersion <= 736)
            return 0x26;
        if (protocolVersion >= 751 && protocolVersion <= 754)
            return 0x25;
        if (protocolVersion >= 755 && protocolVersion <= 755)
            return 0x27;
        if (protocolVersion >= 756 && protocolVersion <= 756)
            return 0x27;
        if (protocolVersion >= 757 && protocolVersion <= 758)
            return 0x27;
        if (protocolVersion >= 759 && protocolVersion <= 759)
            return 0x24;
        if (protocolVersion >= 760 && protocolVersion <= 760)
            return 0x26;
        if (protocolVersion >= 761 && protocolVersion <= 761)
            return 0x25;
        if (protocolVersion >= 762 && protocolVersion <= 763)
            return 0x29;
        if (protocolVersion >= 764 && protocolVersion <= 764)
            return 0x2A;
        if (protocolVersion >= 765 && protocolVersion <= 765)
            return 0x2A;
        if (protocolVersion >= 766 && protocolVersion <= 766)
            return 0x2C;
        if (protocolVersion >= 767 && protocolVersion <= 767)
            return 0x2C;
        if (protocolVersion >= 768 && protocolVersion <= 769)
            return 0x2D;
        if (protocolVersion >= 770 && protocolVersion <= 770)
            return 0x2C;
        if (protocolVersion >= 771 && protocolVersion <= 772)
            return 0x2C;
        throw new System.NotSupportedException($"No packet id for protocol {protocolVersion}.");
    }
}
