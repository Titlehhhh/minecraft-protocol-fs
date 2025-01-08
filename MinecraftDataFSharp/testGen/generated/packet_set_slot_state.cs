namespace MinecraftDataFSharp
{
    public class SetSlotState
    {
        public int SlotId { get; set; }
        public int WindowId { get; set; }
        public bool State { get; set; }

        public sealed class V765_767 : SetSlotState
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int slotId, int windowId, bool state)
            {
                writer.WriteVarInt(slotId);
                writer.WriteVarInt(windowId);
                writer.WriteBoolean(state);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, SlotId, WindowId, State);
            }
        }

        public sealed class V768 : SetSlotState
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int slotId, int windowId, bool state)
            {
                writer.WriteVarInt(slotId);
                writer.WriteVarInt(windowId);
                writer.WriteBoolean(state);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, SlotId, WindowId, State);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V765_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V765_767.SupportedVersion(protocolVersion))
            {
                V765_767.SerializeInternal(writer, SlotId, WindowId, State);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, SlotId, WindowId, State);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}