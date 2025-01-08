namespace MinecraftDataFSharp
{
    public class Pong
    {
        public int Id { get; set; }

        public sealed class V755_768 : Pong
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int id)
            {
                writer.WriteSignedInt(id);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Id);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V755_768.SupportedVersion(protocolVersion))
            {
                V755_768.SerializeInternal(writer, Id);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}