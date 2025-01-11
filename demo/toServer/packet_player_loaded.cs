namespace MinecraftDataFSharp
{
    public class PlayerLoaded : IClientPacket
    {
        public sealed class V769 : PlayerLoaded
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, );
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V769.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V769.SupportedVersion(protocolVersion))
                V769.SerializeInternal(ref writer, protocolVersion);
            else
                throw new Exception();
        }
    }
}