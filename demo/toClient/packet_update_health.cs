namespace MinecraftDataFSharp
{
    public sealed class UpdateHealth
    {
        public float Health { get; set; }
        public int Food { get; set; }
        public float FoodSaturation { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Health = reader.ReadFloat();
            Food = reader.ReadVarInt();
            FoodSaturation = reader.ReadFloat();
        }
    }
}