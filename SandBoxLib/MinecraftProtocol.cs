using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;

namespace SandBoxLib;

public class MinecraftProtocol
{
    private Subject<Packet> _packetSubject = new Subject<Packet>();
    public UseItemSender SendUseItem { get; }

    public IObservable<Packet> OnPacket => _packetSubject;
    public int ProtocolVersion { get; set; }
}

public static class MinecraftProtocolExtensions
{
    public static IObservable<T> OnPacket<T>(this MinecraftProtocol protocol) where T : IPacket, new()
    {
        Console.WriteLine("print enum: " + T.Id);
        return protocol.OnPacket
            .Select(x => new PacketWrapper<T>(x, protocol.ProtocolVersion))
            .Where(x =>
            {
                var id = PacketIdHelper.GetPacketId(protocol.ProtocolVersion, T.Id);
                return x.Id == id;
            })
            .Select(x => x.MainPacket);
    }
}

public struct PacketWrapper<T> where T : IPacket, new()
{
    private Packet _packet;
    private int _protocolVersion;
    public int Id => _packet.Id;

    public T MainPacket
    {
        get
        {
            T packet = new T();
            packet.Serialize(_packet.Data, _protocolVersion);
            return packet;
        }
    }

    public PacketWrapper(Packet packet, int protocolVersion)
    {
        _packet = packet;
        _protocolVersion = protocolVersion;
    }
}

public struct Packet
{
    public Memory<byte> Data;
    public int Id;

    public void Dispose()
    {
    }
}

public interface IPacket
{
    public void Serialize(Memory<byte> data, int protocolVersion);
    public static virtual ClientPacket Id { get; }
}

public class ChatMessagePacket : IPacket
{
    public void Serialize(Memory<byte> data, int protocolVersion)
    {
        throw new NotImplementedException();
    }

    public static ClientPacket Id => ClientPacket.Chat;

    public string Message { get; }

    public sealed class _340_765 : ChatMessagePacket
    {
        public int SequenceId { get; }
    }
}