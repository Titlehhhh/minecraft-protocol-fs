namespace MinecraftDataFSharp
{
    public class SteerBoat
    {
        public bool LeftPaddle { get; set; }
        public bool RightPaddle { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, bool leftPaddle, bool rightPaddle)
        {
            writer.WriteBoolean(leftPaddle);
            writer.WriteBoolean(rightPaddle);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, LeftPaddle, RightPaddle);
        }
    }
}