using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct UpdateHealthPacket(float Health, int Food, float FoodSaturation) : IProtocolType<UpdateHealthPacket>
{
    public static UpdateHealthPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateHealthPacket>(protocolVersion);
        var health = reader.ReadFloat();
        var food = reader.ReadVarInt();
        var foodSaturation = reader.ReadFloat();
        return new UpdateHealthPacket(health, food, foodSaturation);
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<UpdateHealthPacket>(protocolVersion);
        writer.WriteFloat(Health);
        writer.WriteVarInt(Food);
        writer.WriteFloat(FoodSaturation);
    }
}
