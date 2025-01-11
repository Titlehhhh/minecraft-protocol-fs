namespace MinecraftDataFSharp
{
    public class ConfigurationAcknowledged
    {
        public sealed class V764_768 : ConfigurationAcknowledged
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, );
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
                V764_768.SerializeInternal(writer, );
            }
            else
            {
                throw new Exception();
            }
        }
    }
}