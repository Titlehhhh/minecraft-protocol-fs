namespace MinecraftDataFSharp
{
    public sealed class WorldEvent
    {
        public int EffectId { get; set; }
        public Position Location { get; set; }
        public int Data { get; set; }
        public bool Global { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EffectId = reader.ReadSignedInt();
            Location = reader.ReadPosition(protocolVersion);
            Data = reader.ReadSignedInt();
            Global = reader.ReadBoolean();
        }
    }
}