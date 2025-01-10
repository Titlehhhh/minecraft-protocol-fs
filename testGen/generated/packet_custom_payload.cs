namespace MinecraftDataFSharp
{
    public class CustomPayload
    {
        public string Channel { get; set; }
        public byte[] Data { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, string channel, byte[] data)
        {
            writer.WriteString(channel);
            writer.WriteBuffer(data);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, Channel, Data);
        }
    }
}