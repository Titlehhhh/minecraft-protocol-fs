namespace MinecraftDataFSharp
{
    public class Position
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public sealed class V340_767 : Position
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, double x, double y, double z, bool onGround)
            {
                writer.WriteDouble(x);
                writer.WriteDouble(y);
                writer.WriteDouble(z);
                writer.WriteBoolean(onGround);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, X, Y, Z, OnGround);
            }

            public bool OnGround { get; set; }
        }

        public sealed class V768 : Position
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, double x, double y, double z, byte flags)
            {
                writer.WriteDouble(x);
                writer.WriteDouble(y);
                writer.WriteDouble(z);
                writer.WriteUnsignedByte(flags);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, X, Y, Z, Flags);
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
                V340_767.SerializeInternal(writer, X, Y, Z, default);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, X, Y, Z, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}