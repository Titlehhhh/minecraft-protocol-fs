namespace MinecraftDataFSharp
{
    public class UseItem : IClientPacket
    {
        public int Hand { get; set; }

        public sealed class V340_758 : UseItem
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Hand);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int hand)
            {
                writer.WriteVarInt(hand);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 758;
            }
        }

        public sealed class V759_766 : UseItem
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Hand, Sequence);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int hand, int sequence)
            {
                writer.WriteVarInt(hand);
                writer.WriteVarInt(sequence);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 766;
            }

            public int Sequence { get; set; }
        }

        public sealed class V767_769 : UseItem
        {
            public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(ref writer, protocolVersion, Hand, Sequence, Rotation);
            }

            internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int hand, int sequence, Vector2 rotation)
            {
                writer.WriteVarInt(hand);
                writer.WriteVarInt(sequence);
                writer.WriteVector2(rotation, protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 767 and <= 769;
            }

            public int Sequence { get; set; }
            public Vector2 Rotation { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_758.SupportedVersion(protocolVersion) || V759_766.SupportedVersion(protocolVersion) || V767_769.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_758.SupportedVersion(protocolVersion))
                V340_758.SerializeInternal(ref writer, protocolVersion, Hand);
            else if (V759_766.SupportedVersion(protocolVersion))
                V759_766.SerializeInternal(ref writer, protocolVersion, Hand, default);
            else if (V767_769.SupportedVersion(protocolVersion))
                V767_769.SerializeInternal(ref writer, protocolVersion, Hand, default, default);
            else
                throw new Exception();
        }
    }
}