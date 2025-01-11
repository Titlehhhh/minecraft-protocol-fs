namespace MinecraftDataFSharp
{
    public abstract class ChatSuggestions : IServerPacket
    {
        public int Action { get; set; }
        public string[] Entries { get; set; }

        public sealed class V760_769 : ChatSuggestions
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Action = reader.ReadVarInt();
                Entries = reader.ReadArray<string, int>(LengthFormat.VarInt, ReadDelegates.String);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 760 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V760_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}