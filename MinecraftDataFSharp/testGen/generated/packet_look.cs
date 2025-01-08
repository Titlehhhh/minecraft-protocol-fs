namespace MinecraftDataFSharp
{
    public class Look
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public sealed class V340_767 : Look
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, float yaw, float pitch, bool onGround)
            {
                writer.WriteFloat(yaw);
                writer.WriteFloat(pitch);
                writer.WriteBoolean(onGround);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Yaw, Pitch, OnGround);
            }

            public bool OnGround { get; set; }
        }

        public sealed class V768 : Look
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, float yaw, float pitch, byte flags)
            {
                writer.WriteFloat(yaw);
                writer.WriteFloat(pitch);
                writer.WriteUnsignedByte(flags);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Yaw, Pitch, Flags);
            }

            public byte Flags { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_767.SupportedVersion(protocolVersion))
            {
                V340_767.SerializeInternal(writer, Yaw, Pitch, default);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, Yaw, Pitch, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}