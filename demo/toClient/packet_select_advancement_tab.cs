namespace MinecraftDataFSharp
{
    public sealed class SelectAdvancementTab
    {
        public string? Id { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Id = reader.ReadOptional<string?>(ReadDelegates.String);
        }
    }
}