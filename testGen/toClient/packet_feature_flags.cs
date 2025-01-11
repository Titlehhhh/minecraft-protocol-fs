namespace MinecraftDataFSharp
{
    public abstract class FeatureFlags : IServerPacket
    {
        public string[] Features { get; set; }

        public sealed class V761_763 : FeatureFlags
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Features = reader.ReadArray<string, int>(LengthFormat.VarInt, ReadDelegates.String);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 761 and <= 763;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V761_763.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}