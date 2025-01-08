namespace MinecraftDataFSharp
{
    public class EditBook
    {
        public sealed class V393 : EditBook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 393;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Slot newBook, bool signing)
            {
                writer.WriteSlot(newBook, protocolVersion);
                writer.WriteBoolean(signing);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, NewBook, Signing);
            }

            public Slot NewBook { get; set; }
            public bool Signing { get; set; }
        }

        public sealed class V401_755 : EditBook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 401 and <= 755;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Slot newBook, bool signing, int hand)
            {
                writer.WriteSlot(newBook, protocolVersion);
                writer.WriteBoolean(signing);
                writer.WriteVarInt(hand);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, NewBook, Signing, Hand);
            }

            public Slot NewBook { get; set; }
            public bool Signing { get; set; }
            public int Hand { get; set; }
        }

        public sealed class V756_768 : EditBook
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 756 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, string[] pages, string? title)
            {
                writer.WriteVarInt(hand);
                writer.WriteVarInt(pages.Length);
                foreach (var pages_item in pages)
                writer.WriteString(pages_item);
                writer.WriteBoolean(title is not null);
                if (title is not null)
                writer.WriteString(title!);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Pages, Title);
            }

            public int Hand { get; set; }
            public string[] Pages { get; set; }
            public string? Title { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393.SupportedVersion(protocolVersion) || V401_755.SupportedVersion(protocolVersion) || V756_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V393.SupportedVersion(protocolVersion))
            {
                V393.SerializeInternal(writer, default, default);
            }
            else
            {
                if (V401_755.SupportedVersion(protocolVersion))
                {
                    V401_755.SerializeInternal(writer, default, default, default);
                }
                else
                {
                    if (V756_768.SupportedVersion(protocolVersion))
                    {
                        V756_768.SerializeInternal(writer, default, default, default);
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