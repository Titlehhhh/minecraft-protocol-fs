namespace MinecraftDataFSharp
{
    public abstract class ChatPreview : IServerPacket
    {
        public int QueryId { get; set; }
        public string? Message { get; set; }

        public sealed class V759_760 : ChatPreview
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                QueryId = reader.ReadSignedInt();
                Message = reader.ReadOptional<string?>(ReadDelegates.String);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 760;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V759_760.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}