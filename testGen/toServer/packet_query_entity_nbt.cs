namespace MinecraftDataFSharp
{
    public class QueryEntityNbt : IClientPacket
    {
        public int TransactionId { get; set; }
        public int EntityId { get; set; }

        public sealed class V393_769 : QueryEntityNbt
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, TransactionId, EntityId);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int transactionId, int entityId)
            {
                writer.WriteVarInt(transactionId);
                writer.WriteVarInt(entityId);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393_769.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V393_769.SupportedVersion(protocolVersion))
                V393_769.SerializeInternal(ref writer, protocolVersion, TransactionId, EntityId);
            else
                throw new Exception();
        }
    }
}