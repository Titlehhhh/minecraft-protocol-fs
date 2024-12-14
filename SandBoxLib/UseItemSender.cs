using System.Numerics;

namespace SandBoxLib;

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