using System.Numerics;

namespace SandBoxLib;

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
