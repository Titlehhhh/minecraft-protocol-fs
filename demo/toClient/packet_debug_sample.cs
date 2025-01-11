namespace MinecraftDataFSharp
{
    public abstract class DebugSample : IServerPacket
    {
        public long[] Sample { get; set; }
        public int Type { get; set; }

        public sealed class V766_769 : DebugSample
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Sample = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                Type = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 766 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V766_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}