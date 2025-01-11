namespace MinecraftDataFSharp
{
    public sealed class BlockChange
    {
        public Position Location { get; set; }
        public int Type { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Location = reader.ReadPosition(protocolVersion);
            Type = reader.ReadVarInt();
        }
    }
}