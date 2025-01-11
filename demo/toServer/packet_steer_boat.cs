namespace MinecraftDataFSharp
{
    public sealed class SteerBoat
    {
        public bool LeftPaddle { get; set; }
        public bool RightPaddle { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, LeftPaddle, RightPaddle);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, bool leftPaddle, bool rightPaddle)
        {
            writer.WriteBoolean(leftPaddle);
            writer.WriteBoolean(rightPaddle);
        }
    }
}