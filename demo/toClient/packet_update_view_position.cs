namespace MinecraftDataFSharp
{
    public abstract class UpdateViewPosition : IServerPacket
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }

        public sealed class V477_769 : UpdateViewPosition
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadVarInt();
                ChunkZ = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V477_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}