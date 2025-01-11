namespace MinecraftDataFSharp
{
    public class UpdateCommandBlockMinecart
    {
        public int EntityId { get; set; }
        public string Command { get; set; }
        public bool TrackOutput { get; set; }

        public sealed class V393_768 : UpdateCommandBlockMinecart
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int entityId, string command, bool trackOutput)
            {
                writer.WriteVarInt(entityId);
                writer.WriteString(command);
                writer.WriteBoolean(trackOutput);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, EntityId, Command, TrackOutput);
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
                V393_768.SerializeInternal(writer, EntityId, Command, TrackOutput);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}