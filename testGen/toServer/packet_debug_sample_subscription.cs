namespace MinecraftDataFSharp
{
    public class DebugSampleSubscription : IClientPacket
    {
        public int Type { get; set; }

        public sealed class V766_769 : DebugSampleSubscription
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Type);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int type)
            {
                writer.WriteVarInt(type);
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

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V766_769.SupportedVersion(protocolVersion))
                V766_769.SerializeInternal(ref writer, protocolVersion, Type);
            else
                throw new Exception();
        }
    }
}