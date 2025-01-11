namespace MinecraftDataFSharp
{
    public class SteerVehicle : IClientPacket
    {
        public float Sideways { get; set; }
        public float Forward { get; set; }
        public byte Jump { get; set; }

        public sealed class V340_767 : SteerVehicle
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Sideways, Forward, Jump);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, float sideways, float forward, byte jump)
            {
                writer.WriteFloat(sideways);
                writer.WriteFloat(forward);
                writer.WriteUnsignedByte(jump);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_767.SupportedVersion(protocolVersion))
                V340_767.SerializeInternal(ref writer, protocolVersion, Sideways, Forward, Jump);
            else
                throw new Exception();
        }
    }
}