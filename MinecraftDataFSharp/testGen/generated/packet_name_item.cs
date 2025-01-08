namespace MinecraftDataFSharp
{
    public class NameItem
    {
        public string Name { get; set; }

        public sealed class V393_768 : NameItem
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, string name)
            {
                writer.WriteString(name);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Name);
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
                V393_768.SerializeInternal(writer, Name);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}