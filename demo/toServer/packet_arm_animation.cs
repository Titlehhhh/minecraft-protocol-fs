namespace MinecraftDataFSharp
{
    public sealed class ArmAnimation
    {
        public int Hand { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, Hand);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int hand)
        {
            writer.WriteVarInt(hand);
        }
    }
}