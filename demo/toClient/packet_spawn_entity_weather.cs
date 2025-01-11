namespace MinecraftDataFSharp
{
    public abstract class SpawnEntityWeather : IServerPacket
    {
        public int EntityId { get; set; }
        public sbyte Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public sealed class V340_710 : SpawnEntityWeather
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
                Type = reader.ReadSignedByte();
                X = reader.ReadDouble();
                Y = reader.ReadDouble();
                Z = reader.ReadDouble();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 710;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_710.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}