namespace MinecraftDataFSharp
{
    public sealed class GameStateChange
    {
        public byte Reason { get; set; }
        public float GameMode { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Reason = reader.ReadUnsignedByte();
            GameMode = reader.ReadFloat();
        }
    }
}