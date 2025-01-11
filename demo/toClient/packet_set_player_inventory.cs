namespace MinecraftDataFSharp
{
    public abstract class SetPlayerInventory : IServerPacket
    {
        public int SlotId { get; set; }
        public Slot? Contents { get; set; }

        public sealed class V768_769 : SetPlayerInventory
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                SlotId = reader.ReadVarInt();
                Contents = reader.ReadOptional<Slot?>(r_0 => r_0.ReadSlot(protocolVersion));
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}