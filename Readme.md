# Ообертка над PrizmarineJS/minecraft-data

Этот проект является вспомогательным инструментом для **[McProtoNet](https://github.com/Titlehhhh/McProtoNet)**, с помощью которого будет добавлена мультиверсия.


# Основная идея

Это API находится в разработке

## Отправка пакетов

Например, для пакета UseItem будет сгенерировано следующее:

```csharp
public partial struct UseItemSender
{
    public ValueTask Send(int hand)
    {
        if (_currentProtocolVersion is >= 340 and <= 758)
        {
            return UseItemSender_340_758.SendInternal(hand, _packetSender, _currentProtocolVersion);
        }

        if (_currentProtocolVersion is >= 759 and <= 766)
        {
            return UseItemSender_759_766.SendInternal(hand, default, _packetSender, _currentProtocolVersion);
        }

        if (_currentProtocolVersion == 767)
        {
            return UseItemSender_767.SendInternal(hand, default, default, _packetSender, _currentProtocolVersion);
        }

        throw new ProtocolNotSupportException(nameof(ClientPacket.UseItem), _currentProtocolVersion);
    }

    public ValueTask Send_340_758(int hand) =>
        UseItemSender_340_758.SendInternal(hand, _packetSender, _currentProtocolVersion);

    public bool TrySend_340_758(out UseItemSender_340_758 sender)
    {
        if (_currentProtocolVersion >= 340 && _currentProtocolVersion <= 758)
        {
            sender = new UseItemSender_340_758(_packetSender, _currentProtocolVersion);
            return true;
        }

        sender = default;
        return false;
    }

    public ValueTask Send_759_766(int hand, int sequence) =>
        UseItemSender_759_766.SendInternal(hand, sequence, _packetSender, _currentProtocolVersion);

    public bool TrySend_759_766(out UseItemSender_759_766 sender)
    {
        if (_currentProtocolVersion is >= 759 and <= 766)
        {
            sender = new UseItemSender_759_766(_packetSender, _currentProtocolVersion);
            return true;
        }

        sender = default;
        return false;
    }

    public ValueTask Send_767(int hand, int sequence, Vector2 rotation) =>
        UseItemSender_767.SendInternal(hand, sequence, rotation, _packetSender, _currentProtocolVersion);

    public bool TrySend_767(out UseItemSender_767 sender)
    {
        if (_currentProtocolVersion == 767)
        {
            sender = new UseItemSender_767(_packetSender, _currentProtocolVersion);
            return true;
        }

        sender = default;
        return false;
    }
}
```
Также будут сгенерированы всомогательные классы, для каждой версии, например, UseItemSender_340_758

```csharp
public readonly struct UseItemSender_340_758
{
    private readonly IPacketSender _packetSender;
    private readonly int _currentProtocolVersion;

    public UseItemSender_340_758(IPacketSender packetSender, int currentProtocolVersion)
    {
        _packetSender = packetSender;
        _currentProtocolVersion = currentProtocolVersion;
    }

    public ValueTask Send(int hand)
    {
        //Check version
        if (_currentProtocolVersion is >= 340 and <= 758)
        {
            throw new ProtocolNotSupportException(nameof(ClientPacket.UseItem), _currentProtocolVersion);
        }

        return SendInternal(hand, _packetSender, _currentProtocolVersion);
    }

    internal static ValueTask SendInternal(int hand, IPacketSender packetSender, int currentProtocolVersion)
    {
        using var writer = new MinecraftPrimitiveWriter();
        int packetId = PacketIdHelper.GetPacketId(currentProtocolVersion, ClientPacket.UseItem);
        writer.WriteVarInt(packetId);
        writer.WriteVarInt(hand);
        return packetSender.SendPacket(writer.GetWrittenMemory());
    }
}
```
### Клиентский код

```csharp
MinecraftProtocol protocol = new MinecraftProtocol();

// Мульти версионная отправка
await protocol.SendUseItem.Send(hand: 0);

// Определенная версия
if(protocol.SendUseItem.TrySend_759_766(out var sender))
{
    await sender.Send(hand: 0, sequence: 0);
}
```

## Чтение пакетов

В разработке

# Пожертвования

Этот проект полностью поддерживается мной и пока только я понимаю как он работает, поэтому нет смысла делать кому-либо PR'ы кроме меня.