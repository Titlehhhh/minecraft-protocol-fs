namespace MinecraftDataFSharp
{
    public class DebugSampleSubscription
    {
        public int Type { get; set; }

        public sealed class V766_768 : DebugSampleSubscription
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 766 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int type)
            {
                writer.WriteVarInt(type);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Type);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V766_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V766_768.SupportedVersion(protocolVersion))
            {
                V766_768.SerializeInternal(writer, Type);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}