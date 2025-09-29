using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Channels;

namespace SandBoxLib;

public class MinecraftProtocol
{
    private Subject<Packet> _packetSubject = new Subject<Packet>();
    public UseItemSender SendUseItem { get; }

    public ValueTask SendPacket(MemoryOwner<byte> data)
    {
        throw new NotImplementedException();
    }

    public IObservable<Packet> OnPacket => _packetSubject;
    public int ProtocolVersion { get; set; }
}

public class Test
{
    public async Task A()
    {
        MinecraftProtocol protocol = new MinecraftProtocol();

        //protocol.OnPacket<ChatMessageServerPacket.V1>();
    }
}

#region Send

public struct PacketSender<T> where T : IClientPacket, new()
{
    public PacketSender(MinecraftProtocol protocol)
    {
        _protocol = protocol;
    }

    private MinecraftProtocol _protocol;
    public T Packet { get; set; } = new();

    public ValueTask Send()
    {
        //Check null
        if (Packet is null)
        {
            throw new ArgumentNullException(nameof(Packet));
        }

        return _protocol.SendPacket(Packet);
    }
}

public static class WriteExtensions
{
    public static bool TrySend<T>(this MinecraftProtocol protocol, out PacketSender<T> sender)
        where T : IClientPacket, new()
    {
        if (T.SupportedVersion(protocol.ProtocolVersion))
        {
            sender = new PacketSender<T>(protocol);
            return true;
        }

        sender = default;
        return false;
    }

    public static ValueTask SendPacket<T>(this MinecraftProtocol protocol, T packet) where T : IClientPacket
    {
        if (T.SupportedVersion(protocol.ProtocolVersion))
        {
            MinecraftPrimitiveWriter writer = new MinecraftPrimitiveWriter();
            try
            {
                int packetId = PacketIdHelper.GetPacketId(protocol.ProtocolVersion, T.Id);
                writer.WriteVarInt(packetId);
                packet.Serialize(ref writer, protocol.ProtocolVersion);
                return protocol.SendPacket(writer.GetWrittenMemory());
            }
            finally
            {
                writer.Dispose();
            }
        }

        throw new ProtocolNotSupportException(nameof(T.Id), protocol.ProtocolVersion);
    }
}

#endregion

#region Read

public static class MinecraftProtocolExtensions
{
}

public struct Packet
{
    public Memory<byte> Data;
    public int Id;

    public void Dispose()
    {
    }
}

#endregion