public sealed partial class CLASS_NAME : IPacket
{
    public static readonly ProtocolRange[] SupportedVersionsStatic =
    {
        new(MinecraftVersion.StartProtocol, TO1),
        new(FROM2, TO2),
        new(FROM3, MinecraftVersion.LatestProtocol)
    };

    // Common fields (present in ALL versions):
    // public int Hand { get; set; }

    // Version-specific containers (only for ranges that have extra fields):
    // public V759_766Fields? V759_766 { get; set; }
    // public V767_LastFields? V767_Last { get; set; }

    internal void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        switch (protocolVersion)
        {
            // Range with NO version-specific fields — just write common fields:
            case >= MinecraftVersion.StartProtocol and <= TO1:
                // writer.WriteVarInt(CommonField);
                return;

            // Range WITH version-specific fields:
            case >= FROM2 and <= TO2:
            {
                var fields = VERSION_PROP ?? throw new InvalidOperationException("CLASS_NAME FROM2-TO2 fields missing.");
                // writer.WriteVarInt(CommonField);
                // writer.WriteVarInt(fields.ExtraField);
                return;
            }
            case >= FROM3 and <= MinecraftVersion.LatestProtocol:
            {
                var fields = VERSION_PROP2 ?? throw new InvalidOperationException("CLASS_NAME FROM3-last fields missing.");
                // writer.WriteVarInt(CommonField);
                // writer.WriteVarInt(fields.ExtraField);
                return;
            }
            default:
                ThrowHelper.ThrowProtocolNotSupported(nameof(CLASS_NAME), protocolVersion, SupportedVersionsStatic);
                return;
        }
    }

    internal void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        switch (protocolVersion)
        {
            case >= MinecraftVersion.StartProtocol and <= TO1:
                // CommonField = reader.ReadVarInt();
                // V759_766 = null; V767_Last = null;
                return;
            case >= FROM2 and <= TO2:
                // CommonField = reader.ReadVarInt();
                VERSION_PROP = new VERSION_STRUCT { ExtraField = reader.ReadVarInt() };
                // VERSION_PROP2 = null;
                return;
            case >= FROM3 and <= MinecraftVersion.LatestProtocol:
                // CommonField = reader.ReadVarInt();
                VERSION_PROP2 = new VERSION_STRUCT2 { ExtraField = reader.ReadVarInt() };
                // VERSION_PROP = null;
                return;
            default:
                ThrowHelper.ThrowProtocolNotSupported(nameof(CLASS_NAME), protocolVersion, SupportedVersionsStatic);
                return;
        }
    }

    void IPacket.Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        => Serialize(ref writer, protocolVersion);

    void IPacket.Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        => Deserialize(ref reader, protocolVersion);

    // Version-specific structs (ONLY fields that differ — NOT common fields):
    // public struct V759_766Fields { public int Sequence { get; set; } }
    // public struct V767_LastFields { public int Sequence { get; set; } public Vector2 Rotation { get; set; } }
}
