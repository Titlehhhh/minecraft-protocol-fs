namespace MinecraftDataFSharp
{
    public class ChatPreview
    {
        public int Query { get; set; }
        public string Message { get; set; }

        public sealed class V759_760 : ChatPreview
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 760;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int query, string message)
            {
                writer.WriteSignedInt(query);
                writer.WriteString(message);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Query, Message);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V759_760.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V759_760.SupportedVersion(protocolVersion))
            {
                V759_760.SerializeInternal(writer, Query, Message);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}