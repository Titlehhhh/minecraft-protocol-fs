namespace MinecraftDataFSharp
{
    public class EntityAction
    {
        public int EntityId { get; set; }
        public int ActionId { get; set; }
        public int JumpBoost { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int entityId, int actionId, int jumpBoost)
        {
            writer.WriteVarInt(entityId);
            writer.WriteVarInt(actionId);
            writer.WriteVarInt(jumpBoost);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, EntityId, ActionId, JumpBoost);
        }
    }
}