using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class NameItemPacket : IProtocolType<NameItemPacket>
{
    public string Name { get; }

    public NameItemPacket(string name)
    {
        Name = name;
    }

    public static NameItemPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<NameItemPacket>(protocolVersion);
        var name = reader.ReadString();
        return new NameItemPacket(name);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<NameItemPacket>(protocolVersion);
        writer.WriteString(Name);
    }
}
