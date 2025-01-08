namespace MinecraftDataFSharp
{
    public class UpdateCommandBlock
    {
        public Position Location { get; set; }
        public string Command { get; set; }
        public int Mode { get; set; }
        public byte Flags { get; set; }

        public sealed class V393_768 : UpdateCommandBlock
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, string command, int mode, byte flags)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteString(command);
                writer.WriteVarInt(mode);
                writer.WriteUnsignedByte(flags);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Command, Mode, Flags);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V393_768.SupportedVersion(protocolVersion))
            {
                V393_768.SerializeInternal(writer, Location, Command, Mode, Flags);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}