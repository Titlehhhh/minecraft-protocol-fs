namespace MinecraftDataFSharp
{
    public class ChunkBatchReceived : IClientPacket
    {
        public float ChunksPerTick { get; set; }

        public sealed class V764_769 : ChunkBatchReceived
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, ChunksPerTick);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, float chunksPerTick)
            {
                writer.WriteFloat(chunksPerTick);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V764_769.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V764_769.SupportedVersion(protocolVersion))
                V764_769.SerializeInternal(ref writer, protocolVersion, ChunksPerTick);
            else
                throw new Exception();
        }
    }
}