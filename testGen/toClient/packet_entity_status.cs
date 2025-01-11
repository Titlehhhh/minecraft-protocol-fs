namespace MinecraftDataFSharp
{
    public sealed class EntityStatus
    {
        public int EntityId { get; set; }
        public sbyte EntityStatus { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadSignedInt();
            EntityStatus = reader.ReadSignedByte();
        }
    }
}