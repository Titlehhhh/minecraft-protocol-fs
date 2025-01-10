namespace MinecraftDataFSharp
{
    public class ChatSessionUpdate
    {
        public Guid SessionUUID { get; set; }
        public long ExpireTime { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] Signature { get; set; }

        public sealed class V761_765 : ChatSessionUpdate
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 761 and <= 765;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Guid sessionUUID, long expireTime, byte[] publicKey, byte[] signature)
            {
                writer.WriteUUID(sessionUUID);
                writer.WriteSignedLong(expireTime);
                writer.WriteVarInt(publicKey.Length);
                writer.WriteBuffer(publicKey);
                writer.WriteVarInt(signature.Length);
                writer.WriteBuffer(signature);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, SessionUUID, ExpireTime, PublicKey, Signature);
            }
        }

        public sealed class V766_768 : ChatSessionUpdate
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 766 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Guid sessionUUID, long expireTime, byte[] publicKey, byte[] signature)
            {
                writer.WriteUUID(sessionUUID);
                writer.WriteSignedLong(expireTime);
                writer.WriteVarInt(publicKey.Length);
                writer.WriteBuffer(publicKey);
                writer.WriteVarInt(signature.Length);
                writer.WriteBuffer(signature);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, SessionUUID, ExpireTime, PublicKey, Signature);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V761_765.SupportedVersion(protocolVersion) || V766_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V761_765.SupportedVersion(protocolVersion))
            {
                V761_765.SerializeInternal(writer, SessionUUID, ExpireTime, PublicKey, Signature);
            }
            else
            {
                if (V766_768.SupportedVersion(protocolVersion))
                {
                    V766_768.SerializeInternal(writer, SessionUUID, ExpireTime, PublicKey, Signature);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}