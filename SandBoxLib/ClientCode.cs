namespace SandBoxLib;

public abstract class Packet1
{
    internal abstract void Serialize(Stream stream);
}

public class ClientUseItemPacket : Packet1
{
    private ClientUseItemPacket(int hand)
    {
    }

    public int Hand { get; set; }

    public sealed class _340_567 : ClientUseItemPacket
    {
        public _340_567(int hand, int a) : base(hand)
        {
            A = a;
        }

        public int A { get; set; }
    }

    internal override void Serialize(Stream stream)
    {
       
    }
}

public class ClientChatPacket : Packet1
{
    public ClientChatPacket(string message)
    {
    }

    public string Message { get; set; }

    public sealed class _340_567 : ClientChatPacket
    {
        public int SequenceId { get; set; }

        public _340_567(string message, int sequenceId) : base(message)
        {
            SequenceId = sequenceId;
        }
    }

    internal override void Serialize(Stream stream)
    {
        throw new NotImplementedException();
    }
}
