namespace MinecraftDataFSharp
{
    public class TabComplete : IClientPacket
    {
        public string Text { get; set; }

        public sealed class V340 : TabComplete
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Text, AssumeCommand, LookedAtBlock);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, string text, bool assumeCommand, Position? lookedAtBlock)
            {
                writer.WriteString(text);
                writer.WriteBoolean(assumeCommand);
                writer.WriteBoolean(lookedAtBlock is not null);
                if (lookedAtBlock is not null)
                writer.WritePosition(lookedAtBlock!, protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 340;
            }

            public bool AssumeCommand { get; set; }
            public Position? LookedAtBlock { get; set; }
        }

        public sealed class V351_769 : TabComplete
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, TransactionId, Text);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int transactionId, string text)
            {
                writer.WriteVarInt(transactionId);
                writer.WriteString(text);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 351 and <= 769;
            }

            public int TransactionId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340.SupportedVersion(protocolVersion) || V351_769.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340.SupportedVersion(protocolVersion))
                V340.SerializeInternal(ref writer, protocolVersion, Text, default, default);
            else if (V351_769.SupportedVersion(protocolVersion))
                V351_769.SerializeInternal(ref writer, protocolVersion, default, Text);
            else
                throw new Exception();
        }
    }
}