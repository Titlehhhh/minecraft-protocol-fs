namespace MinecraftDataFSharp
{
    public abstract class DestroyEntity : IServerPacket
    {
        public int EntityId { get; set; }

        public sealed class V755 : DestroyEntity
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 755;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}