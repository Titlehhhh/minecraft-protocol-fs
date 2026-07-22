using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct LoginCompressPacket(int Threshold) : IProtocolType<LoginCompressPacket>
{
    public static LoginCompressPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCompressPacket>(protocolVersion);
        var threshold = reader.ReadVarInt();
        return new LoginCompressPacket(threshold);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCompressPacket>(protocolVersion);
        writer.WriteVarInt(Threshold);
    }
}
