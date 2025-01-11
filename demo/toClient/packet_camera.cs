namespace MinecraftDataFSharp
{
    public sealed class Camera
    {
        public int CameraId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            CameraId = reader.ReadVarInt();
        }
    }
}