namespace MinecraftDataFSharp
{
    public class TickEnd
    {
        public sealed class V768 : TickEnd
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
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
            return V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V768.SupportedVersion(protocolVersion))
            {
                V768.SerializeInternal(writer, );
            }
            else
            {
                throw new Exception();
            }
        }
    }
}