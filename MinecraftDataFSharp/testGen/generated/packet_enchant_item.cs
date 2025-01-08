namespace MinecraftDataFSharp
{
    public class EnchantItem
    {
        public sbyte Enchantment { get; set; }

        public sealed class V340_767 : EnchantItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte windowId, sbyte enchantment)
            {
                writer.WriteSignedByte(windowId);
                writer.WriteSignedByte(enchantment);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, Enchantment);
            }

            public sbyte WindowId { get; set; }
        }

        public sealed class V768 : EnchantItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int windowId, sbyte enchantment)
            {
                writer.WriteVarInt(windowId);
                writer.WriteSignedByte(enchantment);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, Enchantment);
            }

            public int WindowId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_767.SupportedVersion(protocolVersion))
            {
                V340_767.SerializeInternal(writer, 0, Enchantment);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, default, Enchantment);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}