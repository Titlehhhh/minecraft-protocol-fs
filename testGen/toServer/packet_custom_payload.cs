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

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, Channel, Data);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, string channel, byte[] data)
        {
            writer.WriteString(channel);
            writer.WriteBuffer(data);
        }
    }
}