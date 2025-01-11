namespace MinecraftDataFSharp
{
    public abstract class AddResourcePack : IServerPacket
    {
        public Guid Uuid { get; set; }
        public string Url { get; set; }
        public string Hash { get; set; }
        public bool Forced { get; set; }
        public NbtTag? PromptMessage { get; set; }

        public sealed class V765_767 : AddResourcePack
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Uuid = reader.ReadUUID();
                Url = reader.ReadString();
                Hash = reader.ReadString();
                Forced = reader.ReadBoolean();
                PromptMessage = reader.ReadOptional<NbtTag?>(r_0 => r_0.ReadNbtTag(false));
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 767;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V765_767.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}