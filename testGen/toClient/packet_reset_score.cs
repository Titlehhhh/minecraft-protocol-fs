namespace MinecraftDataFSharp
{
    public abstract class ResetScore : IServerPacket
    {
        public string EntityName { get; set; }
        public string? ObjectiveName { get; set; }

        public sealed class V765_769 : ResetScore
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityName = reader.ReadString();
                ObjectiveName = reader.ReadOptional<string?>(ReadDelegates.String);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}