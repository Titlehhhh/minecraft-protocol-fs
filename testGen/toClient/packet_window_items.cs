namespace MinecraftDataFSharp
{
    public abstract class WindowItems : IServerPacket
    {
        public Slot[] Items { get; set; }

        public sealed class V340_755 : WindowItems
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                WindowId = reader.ReadUnsignedByte();
                Items = reader.ReadArray<Slot, int>(LengthFormat.Short, r_0 => r_0.ReadSlot(protocolVersion));
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 755;
            }

            public byte WindowId { get; set; }
        }

        public sealed class V756_765 : WindowItems
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                WindowId = reader.ReadUnsignedByte();
                StateId = reader.ReadVarInt();
                Items = reader.ReadArray<Slot, int>(LengthFormat.VarInt, r_0 => r_0.ReadSlot(protocolVersion));
                CarriedItem = reader.ReadSlot(protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 756 and <= 765;
            }

            public byte WindowId { get; set; }
            public int StateId { get; set; }
            public Slot CarriedItem { get; set; }
        }

        public sealed class V766_767 : WindowItems
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                WindowId = reader.ReadUnsignedByte();
                StateId = reader.ReadVarInt();
                Items = reader.ReadArray<Slot, int>(LengthFormat.VarInt, r_0 => r_0.ReadSlot(protocolVersion));
                CarriedItem = reader.ReadSlot(protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 766 and <= 767;
            }

            public byte WindowId { get; set; }
            public int StateId { get; set; }
            public Slot CarriedItem { get; set; }
        }

        public sealed class V768_769 : WindowItems
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                WindowId = reader.ReadVarInt();
                StateId = reader.ReadVarInt();
                Items = reader.ReadArray<Slot, int>(LengthFormat.VarInt, r_0 => r_0.ReadSlot(protocolVersion));
                CarriedItem = reader.ReadSlot(protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }

            public int WindowId { get; set; }
            public int StateId { get; set; }
            public Slot CarriedItem { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_755.SupportedVersion(protocolVersion) || V756_765.SupportedVersion(protocolVersion) || V766_767.SupportedVersion(protocolVersion) || V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}