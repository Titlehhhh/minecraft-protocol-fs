namespace MinecraftDataFSharp
{
    public class Transaction
    {
        public sbyte WindowId { get; set; }
        public short Action { get; set; }
        public bool Accepted { get; set; }

        public sealed class V340_754 : Transaction
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 754;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte windowId, short action, bool accepted)
            {
                writer.WriteSignedByte(windowId);
                writer.WriteSignedShort(action);
                writer.WriteBoolean(accepted);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, Action, Accepted);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_754.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_754.SupportedVersion(protocolVersion))
            {
                V340_754.SerializeInternal(writer, WindowId, Action, Accepted);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}