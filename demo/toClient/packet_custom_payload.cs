namespace MinecraftDataFSharp
{
    public sealed class CustomPayload
    {
        public string Channel { get; set; }
        public byte[] Data { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Channel = reader.ReadString();
            Data = reader.ReadToEnd();
        }
    }
}