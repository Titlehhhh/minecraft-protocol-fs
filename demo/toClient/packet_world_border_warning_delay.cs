namespace MinecraftDataFSharp
{
    public abstract class WorldBorderWarningDelay : IServerPacket
    {
        public int WarningTime { get; set; }

        public sealed class V755_769 : WorldBorderWarningDelay
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                WarningTime = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}