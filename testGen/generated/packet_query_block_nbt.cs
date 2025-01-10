namespace MinecraftDataFSharp
{
    public class QueryBlockNbt
    {
        public int TransactionId { get; set; }
        public Position Location { get; set; }

        public sealed class V393_768 : QueryBlockNbt
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int transactionId, Position location)
            {
                writer.WriteVarInt(transactionId);
                writer.WritePosition(location, protocolVersion);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, TransactionId, Location);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V393_768.SupportedVersion(protocolVersion))
            {
                V393_768.SerializeInternal(writer, TransactionId, Location);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}