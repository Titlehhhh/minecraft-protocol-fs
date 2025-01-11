namespace MinecraftDataFSharp
{
    public class ChunkBatchReceived
    {
        public float ChunksPerTick { get; set; }

        public sealed class V764_768 : ChunkBatchReceived
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, float chunksPerTick)
            {
                writer.WriteFloat(chunksPerTick);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, ChunksPerTick);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V764_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V764_768.SupportedVersion(protocolVersion))
            {
                V764_768.SerializeInternal(writer, ChunksPerTick);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}