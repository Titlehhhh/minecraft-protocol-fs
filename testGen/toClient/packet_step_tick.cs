namespace MinecraftDataFSharp
{
    public abstract class StepTick : IServerPacket
    {
        public int TickSteps { get; set; }

        public sealed class V765_769 : StepTick
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                TickSteps = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}