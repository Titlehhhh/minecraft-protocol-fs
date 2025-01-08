namespace MinecraftDataFSharp
{
    public class PingRequest
    {
        public long Id { get; set; }

        public sealed class V764_768 : PingRequest
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, long id)
            {
                writer.WriteSignedLong(id);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Id);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V764_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V764_768.SupportedVersion(protocolVersion))
            {
                V764_768.SerializeInternal(writer, Id);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}