using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Serverbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct LockDifficultyPacket(bool Locked) : IProtocolType<LockDifficultyPacket>
{
    public static LockDifficultyPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LockDifficultyPacket>(protocolVersion);
        var locked = reader.ReadBoolean();
        return new LockDifficultyPacket(locked);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LockDifficultyPacket>(protocolVersion);
        writer.WriteBoolean(Locked);
    }
}
