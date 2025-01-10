namespace MinecraftDataFSharp
{
    public class BlockDig
    {
        public int Status { get; set; }
        public Position Location { get; set; }
        public sbyte Face { get; set; }

        public sealed class V340_758 : BlockDig
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 758;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int status, Position location, sbyte face)
            {
                writer.WriteVarInt(status);
                writer.WritePosition(location, protocolVersion);
                writer.WriteSignedByte(face);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Status, Location, Face);
            }
        }

        public sealed class V759_768 : BlockDig
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int status, Position location, sbyte face, int sequence)
            {
                writer.WriteVarInt(status);
                writer.WritePosition(location, protocolVersion);
                writer.WriteSignedByte(face);
                writer.WriteVarInt(sequence);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Status, Location, Face, Sequence);
            }

            public int Sequence { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_758.SupportedVersion(protocolVersion) || V759_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_758.SupportedVersion(protocolVersion))
            {
                V340_758.SerializeInternal(writer, Status, Location, Face);
            }
            else
            {
                if (V759_768.SupportedVersion(protocolVersion))
                {
                    V759_768.SerializeInternal(writer, Status, Location, Face, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}