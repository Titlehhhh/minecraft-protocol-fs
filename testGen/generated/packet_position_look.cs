namespace MinecraftDataFSharp
{
    public class PositionLook
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public sealed class V340_767 : PositionLook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, double x, double y, double z, float yaw, float pitch, bool onGround)
            {
                writer.WriteDouble(x);
                writer.WriteDouble(y);
                writer.WriteDouble(z);
                writer.WriteFloat(yaw);
                writer.WriteFloat(pitch);
                writer.WriteBoolean(onGround);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, X, Y, Z, Yaw, Pitch, OnGround);
            }

            public bool OnGround { get; set; }
        }

        public sealed class V768 : PositionLook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, double x, double y, double z, float yaw, float pitch, byte flags)
            {
                writer.WriteDouble(x);
                writer.WriteDouble(y);
                writer.WriteDouble(z);
                writer.WriteFloat(yaw);
                writer.WriteFloat(pitch);
                writer.WriteUnsignedByte(flags);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, X, Y, Z, Yaw, Pitch, Flags);
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
                V340_767.SerializeInternal(writer, X, Y, Z, Yaw, Pitch, default);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, X, Y, Z, Yaw, Pitch, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}