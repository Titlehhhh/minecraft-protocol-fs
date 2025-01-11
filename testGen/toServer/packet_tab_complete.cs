namespace MinecraftDataFSharp
{
    public class TabComplete
    {
        public string Text { get; set; }

        public sealed class V340 : TabComplete
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 340;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, string text, bool assumeCommand, Position? lookedAtBlock)
            {
                writer.WriteString(text);
                writer.WriteBoolean(assumeCommand);
                writer.WriteBoolean(lookedAtBlock is not null);
                if (lookedAtBlock is not null)
                writer.WritePosition(lookedAtBlock!, protocolVersion);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Text, AssumeCommand, LookedAtBlock);
            }

            public bool AssumeCommand { get; set; }
            public Position? LookedAtBlock { get; set; }
        }

        public sealed class V351_768 : TabComplete
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 351 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int transactionId, string text)
            {
                writer.WriteVarInt(transactionId);
                writer.WriteString(text);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, TransactionId, Text);
            }

            public int TransactionId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340.SupportedVersion(protocolVersion) || V351_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340.SupportedVersion(protocolVersion))
            {
                V340.SerializeInternal(writer, Text, default, default);
            }
            else
            {
                if (V351_768.SupportedVersion(protocolVersion))
                {
                    V351_768.SerializeInternal(writer, default, Text);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}