namespace MinecraftDataFSharp
{
    public sealed class Collect
    {
        public int CollectedEntityId { get; set; }
        public int CollectorEntityId { get; set; }
        public int PickupItemCount { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            CollectedEntityId = reader.ReadVarInt();
            CollectorEntityId = reader.ReadVarInt();
            PickupItemCount = reader.ReadVarInt();
        }
    }
}