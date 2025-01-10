namespace MinecraftDataFSharp
{
    public class LockDifficulty
    {
        public bool Locked { get; set; }

        public sealed class V477_768 : LockDifficulty
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, bool locked)
            {
                writer.WriteBoolean(locked);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Locked);
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
                V477_768.SerializeInternal(writer, Locked);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}