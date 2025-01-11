namespace MinecraftDataFSharp
{
    public abstract class UpdateLight : IServerPacket
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }

        public sealed class V477_710 : UpdateLight
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadVarInt();
                ChunkZ = reader.ReadVarInt();
                SkyLightMask = reader.ReadVarInt();
                BlockLightMask = reader.ReadVarInt();
                EmptySkyLightMask = reader.ReadVarInt();
                EmptyBlockLightMask = reader.ReadVarInt();
                Data = reader.ReadToEnd();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 710;
            }

            public int SkyLightMask { get; set; }
            public int BlockLightMask { get; set; }
            public int EmptySkyLightMask { get; set; }
            public int EmptyBlockLightMask { get; set; }
            public byte[] Data { get; set; }
        }

        public sealed class V734_754 : UpdateLight
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadVarInt();
                ChunkZ = reader.ReadVarInt();
                TrustEdges = reader.ReadBoolean();
                SkyLightMask = reader.ReadVarInt();
                BlockLightMask = reader.ReadVarInt();
                EmptySkyLightMask = reader.ReadVarInt();
                EmptyBlockLightMask = reader.ReadVarInt();
                Data = reader.ReadToEnd();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 734 and <= 754;
            }

            public bool TrustEdges { get; set; }
            public int SkyLightMask { get; set; }
            public int BlockLightMask { get; set; }
            public int EmptySkyLightMask { get; set; }
            public int EmptyBlockLightMask { get; set; }
            public byte[] Data { get; set; }
        }

        public sealed class V755_762 : UpdateLight
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadVarInt();
                ChunkZ = reader.ReadVarInt();
                TrustEdges = reader.ReadBoolean();
                SkyLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                BlockLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                EmptySkyLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                EmptyBlockLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                SkyLight = reader.ReadArray<byte[], int>(LengthFormat.VarInt, r_0 => r_0.ReadArray<byte, int>(LengthFormat.VarInt, ReadDelegates.Byte));
                BlockLight = reader.ReadArray<byte[], int>(LengthFormat.VarInt, r_0 => r_0.ReadArray<byte, int>(LengthFormat.VarInt, ReadDelegates.Byte));
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 762;
            }

            public bool TrustEdges { get; set; }
            public long[] SkyLightMask { get; set; }
            public long[] BlockLightMask { get; set; }
            public long[] EmptySkyLightMask { get; set; }
            public long[] EmptyBlockLightMask { get; set; }
            public byte[][] SkyLight { get; set; }
            public byte[][] BlockLight { get; set; }
        }

        public sealed class V763_769 : UpdateLight
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ChunkX = reader.ReadVarInt();
                ChunkZ = reader.ReadVarInt();
                SkyLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                BlockLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                EmptySkyLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                EmptyBlockLightMask = reader.ReadArray<long, int>(LengthFormat.VarInt, ReadDelegates.Int64);
                SkyLight = reader.ReadArray<byte[], int>(LengthFormat.VarInt, r_0 => r_0.ReadArray<byte, int>(LengthFormat.VarInt, ReadDelegates.Byte));
                BlockLight = reader.ReadArray<byte[], int>(LengthFormat.VarInt, r_0 => r_0.ReadArray<byte, int>(LengthFormat.VarInt, ReadDelegates.Byte));
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 763 and <= 769;
            }

            public long[] SkyLightMask { get; set; }
            public long[] BlockLightMask { get; set; }
            public long[] EmptySkyLightMask { get; set; }
            public long[] EmptyBlockLightMask { get; set; }
            public byte[][] SkyLight { get; set; }
            public byte[][] BlockLight { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V477_710.SupportedVersion(protocolVersion) || V734_754.SupportedVersion(protocolVersion) || V755_762.SupportedVersion(protocolVersion) || V763_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}