namespace SandBoxLib;

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