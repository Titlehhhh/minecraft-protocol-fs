namespace MinecraftDataFSharp
{
    public abstract class UnloadChunk : IServerPacket
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }

        public sealed class V340_763 : UnloadChunk
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadSignedInt();
                ChunkZ = reader.ReadSignedInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 763;
            }
        }

        public sealed class V764_769 : UnloadChunk
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkZ = reader.ReadSignedInt();
                ChunkX = reader.ReadSignedInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_763.SupportedVersion(protocolVersion) || V764_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}