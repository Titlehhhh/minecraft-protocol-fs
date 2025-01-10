namespace MinecraftDataFSharp
{
    public class VehicleMove
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, double x, double y, double z, float yaw, float pitch)
        {
            writer.WriteDouble(x);
            writer.WriteDouble(y);
            writer.WriteDouble(z);
            writer.WriteFloat(yaw);
            writer.WriteFloat(pitch);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, X, Y, Z, Yaw, Pitch);
        }
    }
}