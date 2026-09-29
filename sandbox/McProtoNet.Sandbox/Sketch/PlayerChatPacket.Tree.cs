using Dunet;
using McProtoNet.Protocol.Attributes;
using McProtoNet.NBT;
using System;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;

[ProtocolSupport(759, MinecraftVersion.LatestProtocol)]
[Packet("play.toClient.player_chat", PacketPhase.Play, PacketDirection.Clientbound)]
public sealed partial record PlayerChatPacket(
    Guid SenderUuid,
    byte[]? Signature,
    long Timestamp,
    long Salt,
    PlayerChatPacket.UnsignedChatContentValue? UnsignedChatContent,
    PlayerChatPacket.ChatTypeValue ChatType,
    PlayerChatPacket.V759Group? V759 = null,
    PlayerChatPacket.V760Group? V760 = null,
    PlayerChatPacket.V760_LastGroup? V760_Last = null,
    PlayerChatPacket.NetworkGroup? Network = null) : IPacket<PlayerChatPacket>, IPacket
{
    [ProtocolSupport(759, 759)]
    public readonly record struct V759Group(
        string SignedChatContent,
        string SenderName,
        string? SenderTeam);

    [ProtocolSupport(760, 760)]
    public readonly record struct V760Group(
        byte[]? PreviousSignature,
        string? FormattedMessage);

    [ProtocolSupport(760, MinecraftVersion.LatestProtocol)]
    public readonly record struct V760_LastGroup(
        string PlainMessage,
        PreviousMessage[] PreviousMessages,
        [property: ProtocolSupport(761, MinecraftVersion.LatestProtocol)] int? Index,
        [property: ProtocolSupport(770, MinecraftVersion.LatestProtocol)] int? GlobalIndex,
        FilterGroup Filter);

    public readonly record struct FilterGroup(
        int Type,
        long[]? Mask);

    [ProtocolSupport(760, MinecraftVersion.LatestProtocol)]
    public readonly record struct NetworkGroup(
        NetworkNameValue Name,
        NetworkTargetNameValue? TargetName);

    [Union]
    public partial record UnsignedChatContentValue
    {
        [ProtocolSupport(759, 764)] partial record Json(string Value);
        [ProtocolSupport(765, MinecraftVersion.LatestProtocol)] partial record Nbt(NbtTag Value);
    }

    [Union]
    public partial record ChatTypeValue
    {
        [ProtocolSupport(759, 766)] partial record Id(int Value);
        [ProtocolSupport(767, MinecraftVersion.LatestProtocol)] partial record Holder(RegistryOrInline<ChatTypes> Value);
    }

    [Union]
    public partial record NetworkNameValue
    {
        [ProtocolSupport(760, 764)] partial record Json(string Value);
        [ProtocolSupport(765, MinecraftVersion.LatestProtocol)] partial record Nbt(NbtTag Value);
    }

    [Union]
    public partial record NetworkTargetNameValue
    {
        [ProtocolSupport(760, 764)] partial record Json(string Value);
        [ProtocolSupport(765, MinecraftVersion.LatestProtocol)] partial record Nbt(NbtTag Value);
    }
}
