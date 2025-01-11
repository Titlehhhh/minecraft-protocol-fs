namespace MinecraftDataFSharp
{
    public class UseItem
    {
        public int Hand { get; set; }

        public sealed class V340_758 : UseItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 758;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand)
            {
                writer.WriteVarInt(hand);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand);
            }
        }

        public sealed class V759_766 : UseItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 766;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, int sequence)
            {
                writer.WriteVarInt(hand);
                writer.WriteVarInt(sequence);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Sequence);
            }

            public int Sequence { get; set; }
        }

        public sealed class V767_768 : UseItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 767 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, int sequence, Vector2 rotation)
            {
                writer.WriteVarInt(hand);
                writer.WriteVarInt(sequence);
                writer.WriteVector2(rotation, protocolVersion);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Sequence, Rotation);
            }

            public int Sequence { get; set; }
            public Vector2 Rotation { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_758.SupportedVersion(protocolVersion) || V759_766.SupportedVersion(protocolVersion) || V767_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_758.SupportedVersion(protocolVersion))
            {
                V340_758.SerializeInternal(writer, Hand);
            }
            else
            {
                if (V759_766.SupportedVersion(protocolVersion))
                {
                    V759_766.SerializeInternal(writer, Hand, default);
                }
                else
                {
                    if (V767_768.SupportedVersion(protocolVersion))
                    {
                        V767_768.SerializeInternal(writer, Hand, default, default);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
        }
    }
}