namespace MinecraftDataFSharp
{
    public class PickItemFromBlock : IClientPacket
    {
        public Position Position { get; set; }
        public bool IncludeData { get; set; }

        public sealed class V769 : PickItemFromBlock
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Position, IncludeData);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, Position position, bool includeData)
            {
                writer.WritePosition(position, protocolVersion);
                writer.WriteBoolean(includeData);
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
                V769.SerializeInternal(ref writer, protocolVersion, Position, IncludeData);
            else
                throw new Exception();
        }
    }
}