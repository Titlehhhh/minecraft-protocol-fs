using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct UpdateTimePacket(long Age, long Time, bool TickDayTime) : IProtocolType<UpdateTimePacket>
{
    public static UpdateTimePacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateTimePacket>(protocolVersion);
        if (protocolVersion <= 767)
        {
            var age = reader.ReadSignedLong();
            var time = reader.ReadSignedLong();
            return new UpdateTimePacket(age, time, default!);
        }

        if (protocolVersion >= 768)
        {
            var age = reader.ReadSignedLong();
            var time = reader.ReadSignedLong();
            var tickDayTime = reader.ReadBoolean();
            return new UpdateTimePacket(age, time, tickDayTime);
        }

        throw new System.NotSupportedException($"UpdateTimePacket has no wire layout for protocol version {protocolVersion}.");
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateTimePacket>(protocolVersion);
        if (protocolVersion <= 767)
        {
            writer.WriteSignedLong(Age);
            writer.WriteSignedLong(Time);
            return;
        }

        if (protocolVersion >= 768)
        {
            writer.WriteSignedLong(Age);
            writer.WriteSignedLong(Time);
            writer.WriteBoolean(TickDayTime);
            return;
        }

        throw new System.NotSupportedException($"UpdateTimePacket has no wire layout for protocol version {protocolVersion}.");
    }
}
