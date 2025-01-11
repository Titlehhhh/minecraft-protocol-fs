namespace MinecraftDataFSharp
{
    public class ChatPreview : IClientPacket
    {
        public int Query { get; set; }
        public string Message { get; set; }

        public sealed class V759_760 : ChatPreview
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Query, Message);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int query, string message)
            {
                writer.WriteSignedInt(query);
                writer.WriteString(message);
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

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V759_760.SupportedVersion(protocolVersion))
                V759_760.SerializeInternal(ref writer, protocolVersion, Query, Message);
            else
                throw new Exception();
        }
    }
}