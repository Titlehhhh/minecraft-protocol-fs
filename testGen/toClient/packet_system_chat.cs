namespace MinecraftDataFSharp
{
    public abstract class SystemChat : IServerPacket
    {
        public sealed class V759 : SystemChat
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Content = reader.ReadString();
                Type = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 759;
            }

            public string Content { get; set; }
            public int Type { get; set; }
        }

        public sealed class V760_764 : SystemChat
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Content = reader.ReadString();
                IsActionBar = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 760 and <= 764;
            }

            public string Content { get; set; }
            public bool IsActionBar { get; set; }
        }

        public sealed class V765_769 : SystemChat
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Content = reader.ReadNbtTag(false);
                IsActionBar = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }

            public NbtTag Content { get; set; }
            public bool IsActionBar { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V759.SupportedVersion(protocolVersion) || V760_764.SupportedVersion(protocolVersion) || V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}