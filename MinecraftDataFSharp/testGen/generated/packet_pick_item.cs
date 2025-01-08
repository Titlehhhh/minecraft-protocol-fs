namespace MinecraftDataFSharp
{
    public class PickItem
    {
        public int Slot { get; set; }

        public sealed class V393_768 : PickItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int slot)
            {
                writer.WriteVarInt(slot);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Slot);
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
                V393_768.SerializeInternal(writer, Slot);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}