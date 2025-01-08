namespace MinecraftDataFSharp
{
    public class Spectate
    {
        public Guid Target { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Guid target)
        {
            writer.WriteUUID(target);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, Target);
        }
    }
}