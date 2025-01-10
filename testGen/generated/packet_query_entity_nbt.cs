namespace MinecraftDataFSharp
{
    public class QueryEntityNbt
    {
        public int TransactionId { get; set; }
        public int EntityId { get; set; }

        public sealed class V393_768 : QueryEntityNbt
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int transactionId, int entityId)
            {
                writer.WriteVarInt(transactionId);
                writer.WriteVarInt(entityId);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, TransactionId, EntityId);
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
                V393_768.SerializeInternal(writer, TransactionId, EntityId);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}