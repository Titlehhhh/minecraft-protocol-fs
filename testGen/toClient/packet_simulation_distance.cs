namespace MinecraftDataFSharp
{
    public abstract class SimulationDistance : IServerPacket
    {
        public int Distance { get; set; }

        public sealed class V757_769 : SimulationDistance
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Distance = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 757 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V757_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}