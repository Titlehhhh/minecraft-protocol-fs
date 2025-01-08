namespace MinecraftDataFSharp
{
    public class SetDifficulty
    {
        public byte NewDifficulty { get; set; }

        public sealed class V477_768 : SetDifficulty
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, byte newDifficulty)
            {
                writer.WriteUnsignedByte(newDifficulty);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, NewDifficulty);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V477_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V477_768.SupportedVersion(protocolVersion))
            {
                V477_768.SerializeInternal(writer, NewDifficulty);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}