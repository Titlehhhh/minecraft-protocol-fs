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

        if (protocol.TrySend<ChatPacketClient.V1>(out var sender))
        {
            sender.Packet.Message = "Hello World!";
            await sender.Send();
        }
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
    public static bool TrySend<T>(this MinecraftProtocol protocol, out PacketSender<T> sender) where T : IClientPacket,new()
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
            using MinecraftPrimitiveWriter writer = new MinecraftPrimitiveWriter();
            int packetId = PacketIdHelper.GetPacketId(protocol.ProtocolVersion, T.Id);
            writer.WriteVarInt(packetId);
            packet.Serialize(writer, protocol.ProtocolVersion);
            return protocol.SendPacket(writer.GetWrittenMemory());
        }

        throw new ProtocolNotSupportException(nameof(T.Id), protocol.ProtocolVersion);
    }
}

public interface IClientPacket
{
    public void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion);

    public virtual static ClientPacket Id => throw new NotImplementedException();

    public virtual static bool SupportedVersion(int protocolVersion) => throw new NotImplementedException();
}

public class ChatPacketClient : IClientPacket
{
    public string Message { get; set; }
    
    public static bool SupportedVersion(int protocolVersion)
    {
        return protocolVersion is >= 340 and <= 768;
    }

    public sealed class V1 : ChatPacketClient
    {
        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion <= 560;
        }
    }

    public sealed class V2 : ChatPacketClient
    {
        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion > 560;
        }
    }


    public void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
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

public interface IServerPacket
{
    public void Deserialize(Memory<byte> data, int protocolVersion);


    public static virtual ServerPacket Id => throw new NotImplementedException();
    public static virtual IServerPacket Create(int protocolVersion) => throw new NotImplementedException();
    public static virtual bool VersionSupported(int protocolVersion) => throw new NotImplementedException();
}

public abstract class ChatMessageServerPacket : IServerPacket
{
    public abstract void Deserialize(Memory<byte> data, int protocolVersion);

    public static IServerPacket Create(int protocolVersion)
    {
        if (protocolVersion >= 340)
            return new V1();
        return new V2();
    }

    public static bool VersionSupported(int protocolVersion)
    {
        Console.WriteLine("All");
        return protocolVersion >= 340 && protocolVersion <= 768;
    }

    public static ClientPacket Id => ClientPacket.Chat;

    public string Message { get; }

    public sealed class V1 : ChatMessageServerPacket
    {
        public new static bool VersionSupported(int protocolVersion)
        {
            Console.WriteLine("V1");
            return protocolVersion >= 340 && protocolVersion <= 768;
        }

        public int SequenceId { get; }

        public override void Deserialize(Memory<byte> data, int protocolVersion)
        {
            throw new NotImplementedException();
        }
    }

    public class V2 : ChatMessageServerPacket
    {
        public new static bool VersionSupported(int protocolVersion)
        {
            Console.WriteLine("V2");
            return protocolVersion >= 340 && protocolVersion <= 768;
        }

        public int SomeProp { get; }

        public override void Deserialize(Memory<byte> data, int protocolVersion)
        {
            throw new NotImplementedException();
        }
    }
}

#endregion