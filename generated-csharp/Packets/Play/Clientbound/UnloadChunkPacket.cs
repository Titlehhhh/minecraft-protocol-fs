using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct UnloadChunkPacket(int ChunkX, int ChunkZ) : IProtocolType<UnloadChunkPacket>
{
    public static UnloadChunkPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UnloadChunkPacket>(protocolVersion);
        if (protocolVersion <= 763)
        {
            var chunkX = reader.ReadSignedInt();
            var chunkZ = reader.ReadSignedInt();
            return new UnloadChunkPacket(chunkX, chunkZ);
        }

        if (protocolVersion >= 764)
        {
            var chunkZ = reader.ReadSignedInt();
            var chunkX = reader.ReadSignedInt();
            return new UnloadChunkPacket(chunkX, chunkZ);
        }

        throw new System.NotSupportedException($"UnloadChunkPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UnloadChunkPacket>(protocolVersion);
        if (protocolVersion <= 763)
        {
            writer.WriteSignedInt(ChunkX);
            writer.WriteSignedInt(ChunkZ);
            return;
        }

        if (protocolVersion >= 764)
        {
            writer.WriteSignedInt(ChunkZ);
            writer.WriteSignedInt(ChunkX);
            return;
        }

        throw new System.NotSupportedException($"UnloadChunkPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
