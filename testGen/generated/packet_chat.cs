namespace MinecraftDataFSharp
{
    public class Chat
    {
        public string Message { get; set; }

        public sealed class V340_758 : Chat
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 758;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, string message)
            {
                writer.WriteString(message);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Message);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_758.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_758.SupportedVersion(protocolVersion))
            {
                V340_758.SerializeInternal(writer, Message);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}