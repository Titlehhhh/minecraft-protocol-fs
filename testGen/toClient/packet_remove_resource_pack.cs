namespace MinecraftDataFSharp
{
    public abstract class RemoveResourcePack : IServerPacket
    {
        public Guid? Uuid { get; set; }

        public sealed class V765_767 : RemoveResourcePack
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Uuid = reader.ReadOptional<Guid?>(r_0 => r_0.ReadUUID());
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