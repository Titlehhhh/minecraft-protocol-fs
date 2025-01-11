namespace MinecraftDataFSharp
{
    public sealed class Spectate
    {
        public Guid Target { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, Target);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, Guid target)
        {
            writer.WriteUUID(target);
        }
    }
}