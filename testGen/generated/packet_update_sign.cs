namespace MinecraftDataFSharp
{
    public class UpdateSign
    {
        public Position Location { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
        public string Text4 { get; set; }

        public sealed class V340_762 : UpdateSign
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 762;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, string text1, string text2, string text3, string text4)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteString(text1);
                writer.WriteString(text2);
                writer.WriteString(text3);
                writer.WriteString(text4);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Text1, Text2, Text3, Text4);
            }
        }

        public sealed class V763_768 : UpdateSign
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 763 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, bool isFrontText, string text1, string text2, string text3, string text4)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteBoolean(isFrontText);
                writer.WriteString(text1);
                writer.WriteString(text2);
                writer.WriteString(text3);
                writer.WriteString(text4);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, IsFrontText, Text1, Text2, Text3, Text4);
            }

            public bool IsFrontText { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_762.SupportedVersion(protocolVersion) || V763_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_762.SupportedVersion(protocolVersion))
            {
                V340_762.SerializeInternal(writer, Location, Text1, Text2, Text3, Text4);
            }
            else
            {
                if (V763_768.SupportedVersion(protocolVersion))
                {
                    V763_768.SerializeInternal(writer, Location, default, Text1, Text2, Text3, Text4);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}