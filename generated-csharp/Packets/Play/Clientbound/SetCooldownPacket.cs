using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class SetCooldownPacket : IProtocolType<SetCooldownPacket>
{
    public int ItemId { get; }
    public string CooldownGroup { get; }
    public int CooldownTicks { get; }

    public SetCooldownPacket(int itemId, string cooldownGroup, int cooldownTicks)
    {
        ItemId = itemId;
        CooldownGroup = cooldownGroup;
        CooldownTicks = cooldownTicks;
    }

    public static SetCooldownPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SetCooldownPacket>(protocolVersion);
        if (protocolVersion <= 767)
        {
            var itemId = reader.ReadVarInt();
            var cooldownTicks = reader.ReadVarInt();
            return new SetCooldownPacket(itemId, default!, cooldownTicks);
        }

        if (protocolVersion >= 768)
        {
            var cooldownGroup = reader.ReadString();
            var cooldownTicks = reader.ReadVarInt();
            return new SetCooldownPacket(default!, cooldownGroup, cooldownTicks);
        }

        throw new System.NotSupportedException($"SetCooldownPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SetCooldownPacket>(protocolVersion);
        if (protocolVersion <= 767)
        {
            writer.WriteVarInt(ItemId);
            writer.WriteVarInt(CooldownTicks);
            return;
        }

        if (protocolVersion >= 768)
        {
            writer.WriteString(CooldownGroup);
            writer.WriteVarInt(CooldownTicks);
            return;
        }

        throw new System.NotSupportedException($"SetCooldownPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
