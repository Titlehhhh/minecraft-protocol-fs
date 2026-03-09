{{usages}}

{{attributes}}
public sealed partial class CLASS_NAME : IPacket
{
    internal void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        switch (protocolVersion)
        {
            case >= MinecraftVersion.StartProtocol and <= TO1:
                return;

            case >= FROM2 and <= TO2:
            {
                var fields = VERSION_PROP ?? throw new InvalidOperationException("CLASS_NAME FROM2-TO2 fields missing.");
                return;
            }
            case >= FROM3 and <= MinecraftVersion.LatestProtocol:
            {
                var fields = VERSION_PROP2 ?? throw new InvalidOperationException("CLASS_NAME FROM3-last fields missing.");
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
                return;
            case >= FROM2 and <= TO2:
                VERSION_PROP = new VERSION_STRUCT { ExtraField = reader.ReadVarInt() };
                return;
            case >= FROM3 and <= MinecraftVersion.LatestProtocol:
                VERSION_PROP2 = new VERSION_STRUCT2 { ComplexField = reader.ReadType<SomeType>(protocolVersion) };
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

    public struct V_FROM2_TO2Fields { public int ExtraField { get; set; } }
    public struct V_FROM3_LastFields { public int ExtraField { get; set; } public SomeType ComplexField { get; set; } }
}
