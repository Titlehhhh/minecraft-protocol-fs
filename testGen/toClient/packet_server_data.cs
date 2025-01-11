namespace MinecraftDataFSharp
{
    public abstract class ServerData : IServerPacket
    {
        public sealed class V759 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadOptional<string?>(ReadDelegates.String);
                Icon = reader.ReadOptional<string?>(ReadDelegates.String);
                PreviewsChat = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 759;
            }

            public string? Motd { get; set; }
            public string? Icon { get; set; }
            public bool PreviewsChat { get; set; }
        }

        public sealed class V760 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadOptional<string?>(ReadDelegates.String);
                Icon = reader.ReadOptional<string?>(ReadDelegates.String);
                PreviewsChat = reader.ReadBoolean();
                EnforcesSecureChat = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 760;
            }

            public string? Motd { get; set; }
            public string? Icon { get; set; }
            public bool PreviewsChat { get; set; }
            public bool EnforcesSecureChat { get; set; }
        }

        public sealed class V761 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadOptional<string?>(ReadDelegates.String);
                Icon = reader.ReadOptional<string?>(ReadDelegates.String);
                EnforcesSecureChat = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 761;
            }

            public string? Motd { get; set; }
            public string? Icon { get; set; }
            public bool EnforcesSecureChat { get; set; }
        }

        public sealed class V762_764 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadString();
                IconBytes = reader.ReadOptional<byte[]?>(r_0 => r_0.ReadBuffer(LengthFormat.VarInt));
                EnforcesSecureChat = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 762 and <= 764;
            }

            public string Motd { get; set; }
            public byte[]? IconBytes { get; set; }
            public bool EnforcesSecureChat { get; set; }
        }

        public sealed class V765 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadNbtTag(false);
                IconBytes = reader.ReadOptional<byte[]?>(r_0 => r_0.ReadBuffer(LengthFormat.VarInt));
                EnforcesSecureChat = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 765;
            }

            public NbtTag Motd { get; set; }
            public byte[]? IconBytes { get; set; }
            public bool EnforcesSecureChat { get; set; }
        }

        public sealed class V766_769 : ServerData
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Motd = reader.ReadNbtTag(false);
                IconBytes = reader.ReadOptional<byte[]?>(r_0 => r_0.ReadBuffer());
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 766 and <= 769;
            }

            public NbtTag Motd { get; set; }
            public byte[]? IconBytes { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V759.SupportedVersion(protocolVersion) || V760.SupportedVersion(protocolVersion) || V761.SupportedVersion(protocolVersion) || V762_764.SupportedVersion(protocolVersion) || V765.SupportedVersion(protocolVersion) || V766_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}