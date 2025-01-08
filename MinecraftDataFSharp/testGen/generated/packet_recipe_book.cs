namespace MinecraftDataFSharp
{
    public class RecipeBook
    {
        public int BookId { get; set; }
        public bool BookOpen { get; set; }
        public bool FilterActive { get; set; }

        public sealed class V751_768 : RecipeBook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 751 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int bookId, bool bookOpen, bool filterActive)
            {
                writer.WriteVarInt(bookId);
                writer.WriteBoolean(bookOpen);
                writer.WriteBoolean(filterActive);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, BookId, BookOpen, FilterActive);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V751_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V751_768.SupportedVersion(protocolVersion))
            {
                V751_768.SerializeInternal(writer, BookId, BookOpen, FilterActive);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}