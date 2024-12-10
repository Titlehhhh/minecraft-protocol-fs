using System.Collections.Frozen;
using System.Numerics;

namespace ConsoleApp1;

public class ProtocolNotSupportException(string packetName, int protocolVersion) : Exception;

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

public readonly struct UseItemSender_759_766
{
    private readonly IPacketSender _packetSender;
    private readonly int _currentProtocolVersion;

    public UseItemSender_759_766(IPacketSender packetSender, int currentProtocolVersion)
    {
        _packetSender = packetSender;
        _currentProtocolVersion = currentProtocolVersion;
    }

    public ValueTask Send(int hand, int sequence)
    {
        //Check version
        if (_currentProtocolVersion is >= 759 and <= 766)
        {
            throw new ProtocolNotSupportException(nameof(ClientPacket.UseItem), _currentProtocolVersion);
        }

        return SendInternal(hand, sequence, _packetSender, _currentProtocolVersion);
    }

    internal static ValueTask SendInternal(int hand, int sequence, IPacketSender packetSender,
        int currentProtocolVersion)
    {
        using var writer = new MinecraftPrimitiveWriter();
        int packetId = PacketIdHelper.GetPacketId(currentProtocolVersion, ClientPacket.UseItem);
        writer.WriteVarInt(packetId);
        writer.WriteVarInt(hand);
        writer.WriteVarInt(sequence);
        return packetSender.SendPacket(writer.GetWrittenMemory());
    }
}

public readonly struct UseItemSender_767
{
    private readonly IPacketSender _packetSender;
    private readonly int _currentProtocolVersion;

    public UseItemSender_767(IPacketSender packetSender, int currentProtocolVersion)
    {
        _packetSender = packetSender;
        _currentProtocolVersion = currentProtocolVersion;
    }

    public ValueTask Send(int hand, int sequence, Vector2 rotation)
    {
        //Check version
        if (_currentProtocolVersion == 767)
        {
            throw new ProtocolNotSupportException(nameof(ClientPacket.UseItem), _currentProtocolVersion);
        }

        return SendInternal(hand, sequence, rotation, _packetSender, _currentProtocolVersion);
    }

    internal static ValueTask SendInternal(int hand, int sequence, Vector2 rotation, IPacketSender packetSender,
        int currentProtocolVersion)
    {
        using var writer = new MinecraftPrimitiveWriter();
        int packetId = PacketIdHelper.GetPacketId(currentProtocolVersion, ClientPacket.UseItem);
        writer.WriteVarInt(packetId);
        writer.WriteVarInt(hand);
        writer.WriteVarInt(sequence);
        writer.WriteVector2f(rotation);
        return packetSender.SendPacket(writer.GetWrittenMemory());
    }
}



public class MinecraftProtocol
{
    public UseItemSender SendUseItem { get; }
}

class ClientCode
{
    async Task RunBot()
    {
        MinecraftProtocol protocol = new MinecraftProtocol();
        
        if(protocol.SendUseItem.TrySend_759_766(out var sender))
        {
            await sender.Send(5, 6);
        }

        await protocol.SendUseItem.Send(5);
    }
}


public static class PacketIdHelper
{
    // client packet id maps
    private static readonly FrozenDictionary<long, int> ClientPacketIdMap;

    public static int GetPacketId(int protocol, ClientPacket packet)
    {
        int packetAsInt = (int)packet;
        return ClientPacketIdMap[Combine(protocol, packetAsInt)];
    }

    private static long Combine(int a, int b)
    {
        //Первые 4 байта a,вторые 4 байта b
        return (long)a << 32 | (uint)b;
    }
}