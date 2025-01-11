namespace MinecraftDataFSharp
{
    public sealed class SetPassengers
    {
        public int EntityId { get; set; }
        public int[] Passengers { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadVarInt();
            Passengers = reader.ReadArray<int, int>(LengthFormat.VarInt, ReadDelegates.VarInt);
        }
    }
}