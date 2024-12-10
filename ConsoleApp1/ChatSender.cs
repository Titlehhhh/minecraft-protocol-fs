namespace ConsoleApp1;

public partial struct UseItemSender
{
    private IPacketSender _packetSender;

    private int _currentProtocolVersion;

    public UseItemSender(IPacketSender packetSender, int currentProtocolVersion)
    {
        _packetSender = packetSender;
        _currentProtocolVersion = currentProtocolVersion;
    }
}