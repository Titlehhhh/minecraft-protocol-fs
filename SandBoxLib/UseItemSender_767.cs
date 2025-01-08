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

public class BlockPlace : IClientPacket
{
    public Position Location { get; set; }
    public int Direction { get; set; }
    public int Hand { get; set; }
    public float CursorX { get; set; }
    public float CursorY { get; set; }
    public float CursorZ { get; set; }

    public sealed class V340_404 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 404;
        }
    }

    public sealed class V477_758 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 477 and <= 758;
        }

        public bool InsideBlock { get; set; }
    }

    public sealed class V759_767 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 759 and <= 767;
        }

        public bool InsideBlock { get; set; }
        public int Sequence { get; set; }
    }

    public sealed class V768 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 768 and <= 768;
        }

        public bool InsideBlock { get; set; }
        public bool WorldBorderHit { get; set; }
        public int Sequence { get; set; }
    }

    public void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        throw new NotImplementedException();
    }

    
}