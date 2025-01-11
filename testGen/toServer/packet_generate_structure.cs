namespace MinecraftDataFSharp
{
    public class GenerateStructure
    {
        public Position Location { get; set; }
        public int Levels { get; set; }
        public bool KeepJigsaws { get; set; }

        public sealed class V734_768 : GenerateStructure
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 734 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, int levels, bool keepJigsaws)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteVarInt(levels);
                writer.WriteBoolean(keepJigsaws);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Levels, KeepJigsaws);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V734_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V734_768.SupportedVersion(protocolVersion))
            {
                V734_768.SerializeInternal(writer, Location, Levels, KeepJigsaws);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}