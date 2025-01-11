namespace MinecraftDataFSharp
{
    public sealed class AttachEntity
    {
        public int EntityId { get; set; }
        public int VehicleId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadSignedInt();
            VehicleId = reader.ReadSignedInt();
        }
    }
}