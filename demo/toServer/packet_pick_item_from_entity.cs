namespace MinecraftDataFSharp
{
    public class PickItemFromEntity : IClientPacket
    {
        public int EntityId { get; set; }
        public bool IncludeData { get; set; }

        public sealed class V769 : PickItemFromEntity
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, EntityId, IncludeData);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int entityId, bool includeData)
            {
                writer.WriteVarInt(entityId);
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
                V769.SerializeInternal(ref writer, protocolVersion, EntityId, IncludeData);
            else
                throw new Exception();
        }
    }
}