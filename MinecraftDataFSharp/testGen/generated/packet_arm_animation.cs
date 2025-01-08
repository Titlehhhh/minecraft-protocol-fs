namespace MinecraftDataFSharp
{
    public class ArmAnimation
    {
        public int Hand { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand)
        {
            writer.WriteVarInt(hand);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, Hand);
        }
    }
}