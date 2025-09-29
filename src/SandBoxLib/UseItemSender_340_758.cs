namespace SandBoxLib;

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