namespace MinecraftDataFSharp
{
    public class SelectBundleItem
    {
        public int SlotId { get; set; }
        public int SelectedItemIndex { get; set; }

        public sealed class V768 : SelectBundleItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int slotId, int selectedItemIndex)
            {
                writer.WriteVarInt(slotId);
                writer.WriteVarInt(selectedItemIndex);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, SlotId, SelectedItemIndex);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V768.SupportedVersion(protocolVersion))
            {
                V768.SerializeInternal(writer, SlotId, SelectedItemIndex);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}