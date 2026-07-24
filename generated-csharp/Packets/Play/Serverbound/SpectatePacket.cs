using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;
using System;

namespace McProtoNet.Protocol.Packets.Play.Serverbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct SpectatePacket(Guid Target) : IProtocolType<SpectatePacket>
{
    public static SpectatePacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SpectatePacket>(protocolVersion);
        var target = reader.ReadUUID();
        return new SpectatePacket(target);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SpectatePacket>(protocolVersion);
        writer.WriteUUID(Target);
    }
}
