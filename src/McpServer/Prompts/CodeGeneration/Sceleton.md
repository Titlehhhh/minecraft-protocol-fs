{{Attributes}}
public sealed partial class {{PacketName}} : {{BaseClasses}}
{

    // =====================================================================
    // Common fields
    // =====================================================================
    // These fields exist in ALL protocol versions.
    // They are always read/written before version-specific fields.
    //
    // Example:
    // public float Yaw { get; set; }
    // public float Pitch { get; set; }


    // =====================================================================
    // Version-specific field containers
    // =====================================================================
    // Exactly ONE of these properties must be non-null at runtime,
    // depending on the protocol version.
    //
    // Naming convention:
    // - V<From>_<To>Fields
    // - Use 'Last' for the latest protocol range.
    //
    // Example:
    // public VFirst_767Fields? VFirst_767 { get; set; }
    // public V768_LastFields? V768_Last { get; set; }


    // =====================================================================
    // Serialization
    // =====================================================================
    // Rules:
    // - Select version by protocolVersion
    // - Read common fields first
    // - Then read/write version-specific fields
    // - Throw if version-specific fields are missing
    internal void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        switch (protocolVersion)
        {
            // Example:
            // case >= <from> and <= <to>:
            // {
            //     var fields = <VersionProperty>
            //         ?? throw new InvalidOperationException("<PacketName> <VersionName> fields missing.");
            //
            //     // Write common fields
            //     writer.WriteFloat(Yaw);
            //     writer.WriteFloat(Pitch);
            //
            //     // Write version-specific fields
            //     writer.WriteBoolean(fields.OnGround);
            //     return;
            // }

            default:
                ThrowHelper.ThrowProtocolNotSupported(
                    nameof(<PacketClassName>),
                    protocolVersion,
                    SupportedVersionsStatic);
                return;
        }
    }

    // =====================================================================
    // Deserialization
    // =====================================================================
    // Rules:
    // - Select version by protocolVersion
    // - Read common fields first
    // - Instantiate ONLY the matching version-specific structure
    // - Set all other version-specific properties to null
    internal void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        switch (protocolVersion)
        {
            // Example:
            // case >= <from> and <= <to>:
            //     // Read common fields
            //     Yaw = reader.ReadFloat();
            //     Pitch = reader.ReadFloat();
            //
            //     // Read version-specific fields
            //     <VersionProperty> = new <VersionStruct>
            //     {
            //         OnGround = reader.ReadBoolean()
            //     };
            //
            //     // Reset other version structures
            //     <OtherVersionProperty> = null;
            //     return;

            default:
                ThrowHelper.ThrowProtocolNotSupported(
                    nameof(<PacketClassName>),
                    protocolVersion,
                    SupportedVersionsStatic);
                return;
        }
    }

    // =====================================================================
    // Interface forwarding
    // =====================================================================
    void IPacket.Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        => Serialize(ref writer, protocolVersion);

    void IPacket.Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        => Deserialize(ref reader, protocolVersion);

    // =====================================================================
    // Version-specific field structures
    // =====================================================================
    // These structs contain ONLY fields that differ between protocol versions.
    // They must NOT include common fields.
    //
    // Example:
    // public struct VFirst_767Fields
    // {
    //     public bool OnGround { get; set; }
    // }
    //
    // public struct V768_LastFields
    // {
    //     public byte Flags { get; set; }
    // }
}