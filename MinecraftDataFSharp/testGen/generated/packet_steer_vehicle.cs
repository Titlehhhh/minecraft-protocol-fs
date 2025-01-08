namespace MinecraftDataFSharp
{
    public class SteerVehicle
    {
        public float Sideways { get; set; }
        public float Forward { get; set; }
        public byte Jump { get; set; }

        public sealed class V340_767 : SteerVehicle
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, float sideways, float forward, byte jump)
            {
                writer.WriteFloat(sideways);
                writer.WriteFloat(forward);
                writer.WriteUnsignedByte(jump);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Sideways, Forward, Jump);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_767.SupportedVersion(protocolVersion))
            {
                V340_767.SerializeInternal(writer, Sideways, Forward, Jump);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}