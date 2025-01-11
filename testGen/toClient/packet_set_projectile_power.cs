namespace MinecraftDataFSharp
{
    public abstract class SetProjectilePower : IServerPacket
    {
        public int Id { get; set; }

        public sealed class V766 : SetProjectilePower
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Id = reader.ReadVarInt();
                Power = reader.ReadVector364(protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 766;
            }

            public Vector3F64 Power { get; set; }
        }

        public sealed class V767_769 : SetProjectilePower
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Id = reader.ReadVarInt();
                AccelerationPower = reader.ReadDouble();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 767 and <= 769;
            }

            public double AccelerationPower { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V766.SupportedVersion(protocolVersion) || V767_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}