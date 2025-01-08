namespace MinecraftDataFSharp
{
    public class BlockPlace
    {
        public Position Location { get; set; }
        public int Direction { get; set; }
        public int Hand { get; set; }
        public float CursorX { get; set; }
        public float CursorY { get; set; }
        public float CursorZ { get; set; }

        public sealed class V340_404 : BlockPlace
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 404;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, int direction, int hand, float cursorX, float cursorY, float cursorZ)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteVarInt(direction);
                writer.WriteVarInt(hand);
                writer.WriteFloat(cursorX);
                writer.WriteFloat(cursorY);
                writer.WriteFloat(cursorZ);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Direction, Hand, CursorX, CursorY, CursorZ);
            }
        }

        public sealed class V477_758 : BlockPlace
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 758;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, Position location, int direction, float cursorX, float cursorY, float cursorZ, bool insideBlock)
            {
                writer.WriteVarInt(hand);
                writer.WritePosition(location, protocolVersion);
                writer.WriteVarInt(direction);
                writer.WriteFloat(cursorX);
                writer.WriteFloat(cursorY);
                writer.WriteFloat(cursorZ);
                writer.WriteBoolean(insideBlock);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Location, Direction, CursorX, CursorY, CursorZ, InsideBlock);
            }

            public bool InsideBlock { get; set; }
        }

        public sealed class V759_767 : BlockPlace
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, Position location, int direction, float cursorX, float cursorY, float cursorZ, bool insideBlock, int sequence)
            {
                writer.WriteVarInt(hand);
                writer.WritePosition(location, protocolVersion);
                writer.WriteVarInt(direction);
                writer.WriteFloat(cursorX);
                writer.WriteFloat(cursorY);
                writer.WriteFloat(cursorZ);
                writer.WriteBoolean(insideBlock);
                writer.WriteVarInt(sequence);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Location, Direction, CursorX, CursorY, CursorZ, InsideBlock, Sequence);
            }

            public bool InsideBlock { get; set; }
            public int Sequence { get; set; }
        }

        public sealed class V768 : BlockPlace
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int hand, Position location, int direction, float cursorX, float cursorY, float cursorZ, bool insideBlock, bool worldBorderHit, int sequence)
            {
                writer.WriteVarInt(hand);
                writer.WritePosition(location, protocolVersion);
                writer.WriteVarInt(direction);
                writer.WriteFloat(cursorX);
                writer.WriteFloat(cursorY);
                writer.WriteFloat(cursorZ);
                writer.WriteBoolean(insideBlock);
                writer.WriteBoolean(worldBorderHit);
                writer.WriteVarInt(sequence);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Hand, Location, Direction, CursorX, CursorY, CursorZ, InsideBlock, WorldBorderHit, Sequence);
            }

            public bool InsideBlock { get; set; }
            public bool WorldBorderHit { get; set; }
            public int Sequence { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_404.SupportedVersion(protocolVersion) || V477_758.SupportedVersion(protocolVersion) || V759_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_404.SupportedVersion(protocolVersion))
            {
                V340_404.SerializeInternal(writer, Location, Direction, Hand, CursorX, CursorY, CursorZ);
            }
            else
            {
                if (V477_758.SupportedVersion(protocolVersion))
                {
                    V477_758.SerializeInternal(writer, Hand, Location, Direction, CursorX, CursorY, CursorZ, default);
                }
                else
                {
                    if (V759_767.SupportedVersion(protocolVersion))
                    {
                        V759_767.SerializeInternal(writer, Hand, Location, Direction, CursorX, CursorY, CursorZ, default, default);
                    }
                    else
                    {
                        if (V768.SupportedVersion(protocolVersion))
                        {
                            V768.SerializeInternal(writer, Hand, Location, Direction, CursorX, CursorY, CursorZ, default, default, default);
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
}