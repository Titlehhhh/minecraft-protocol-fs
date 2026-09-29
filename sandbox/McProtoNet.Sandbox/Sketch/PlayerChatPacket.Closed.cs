using McProtoNet.Protocol.Attributes;
using McProtoNet.NBT;
using System;
using System.Runtime.CompilerServices;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;

// Дерево форм. Узел = диапазон версий. Поле живёт на самом верхнем узле,
// где оно есть во всех версиях ниже. Лист = одна форма провода.
// Поля — required init: узел объявляет своё один раз, наследник не повторяет.

[ProtocolSupport(759, MinecraftVersion.LatestProtocol)]
[Packet("play.toClient.player_chat", PacketPhase.Play, PacketDirection.Clientbound)]
public
#if !NET11_0_OR_GREATER
abstract
#endif
partial record PlayerChatPacket : IPacket
{
    private PlayerChatPacket() { }

    public required Guid SenderUuid { get; init; }
    public required byte[]? Signature { get; init; }
    public required long Timestamp { get; init; }
    public required long Salt { get; init; }

    // ---- лист 759 ----
    [ProtocolSupport(759, 759)]
    public sealed record V759 : PlayerChatPacket
    {
        public required string SignedChatContent { get; init; }
        public required string SenderName { get; init; }
        public required string? SenderTeam { get; init; }
        public required string? UnsignedChatContentJson { get; init; }
        public required int Type { get; init; }
    }

    // ---- узел 760+ ----
    [ProtocolSupport(760, MinecraftVersion.LatestProtocol)]
    public
#if !NET11_0_OR_GREATER
    abstract
#endif
    partial record Modern : PlayerChatPacket
    {
        private Modern() { }

        public required string PlainMessage { get; init; }
        public required PreviousMessage[] PreviousMessages { get; init; }
        public required int FilterType { get; init; }
        public required long[]? FilterTypeMask { get; init; }
        public required ChatText UnsignedChatContent { get; init; }
        public required ChatText NetworkName { get; init; }
        public required ChatText? NetworkTargetName { get; init; }

        [ProtocolSupport(760, 760)]
        public sealed record V760 : Modern
        {
            public required byte[]? PreviousSignature { get; init; }
            public required string? FormattedMessage { get; init; }
            public required int Type { get; init; }
        }

        // ---- узел 761+ ----
        [ProtocolSupport(761, MinecraftVersion.LatestProtocol)]
        public
#if !NET11_0_OR_GREATER
        abstract
#endif
        partial record Indexed : Modern
        {
            private Indexed() { }

            public required int Index { get; init; }

            [ProtocolSupport(761, 766)]
            public sealed record V761_766 : Indexed
            {
                public required int Type { get; init; }
            }

            [ProtocolSupport(767, 769)]
            public sealed record V767_769 : Indexed
            {
                public required RegistryOrInline<ChatTypes> ChatType { get; init; }
            }

            [ProtocolSupport(770, MinecraftVersion.LatestProtocol)]
            public sealed record V770 : Indexed
            {
                public required RegistryOrInline<ChatTypes> ChatType { get; init; }
                public required int GlobalIndex { get; init; }
            с
        }
    }

    public static PlayerChatPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion) => throw null!;

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfShapeNotForVersion(this, protocolVersion);
        throw null!;
    }
}

#if NET11_0_OR_GREATER
closed partial record PlayerChatPacket;
closed partial record PlayerChatPacket.Modern;
closed partial record PlayerChatPacket.Modern.Indexed;
#endif

// Поле, у которого менялся тип провода: JSON-строка до 764, NBT с 765.
// Рукописный union из генератора: без бокса, без null-ветки.
[Union]
public readonly struct ChatText
{
    private readonly byte _tag;
    private readonly string? _json;
    private readonly NbtTag? _nbt;

    public ChatText(string json) { _tag = 1; _json = json; _nbt = null; }
    public ChatText(NbtTag nbt)  { _tag = 2; _json = null; _nbt = nbt; }

    public bool HasValue => _tag != 0;
    public object Value => _tag == 1 ? _json! : _nbt!;
    public bool TryGetValue(out string value) { value = _json!; return _tag == 1; }
    public bool TryGetValue(out NbtTag value) { value = _nbt!;  return _tag == 2; }
}

// ---------- как это выглядит у пользователя ----------

static class Usage
{
    static void Handle(PlayerChatPacket packet)
    {
        var who = packet.SenderUuid;                          // общее: всегда, без ветвления

        if (packet is PlayerChatPacket.Modern m)              // с 760: одна проверка на весь узел
        {
            var text = m.PlainMessage;
            var name = m.NetworkName switch                   // тип менялся: union, полный switch
            {
                string json => json,
                NbtTag nbt  => nbt.ToString(),
            };
        }

        var index = packet switch                             // по листам: пропуск = CS8509
        {
            PlayerChatPacket.V759 => -1,
            PlayerChatPacket.Modern.V760 => -1,
            PlayerChatPacket.Modern.Indexed i => i.Index,     // узел покрывает три листа разом
        };
    }

    static PlayerChatPacket Build()
    {
        // отправка: пропущенное required-поле = CS9035, половины не бывает
        return new PlayerChatPacket.Modern.Indexed.V770
        {
            SenderUuid = Guid.NewGuid(), Signature = null, Timestamp = 0, Salt = 0,
            PlainMessage = "hi", PreviousMessages = [], FilterType = 0, FilterTypeMask = null,
            UnsignedChatContent = new ChatText(NbtTag.Empty), NetworkName = new ChatText(NbtTag.Empty), NetworkTargetName = null,
            Index = 0, ChatType = default, GlobalIndex = 0,
        };
    }

    static PlayerChatPacket Retarget(PlayerChatPacket.Modern.Indexed.V770 p)
    {
        // копия с правкой: with работает по всему дереву
        return p with { PlainMessage = "edited" };
    }
}
