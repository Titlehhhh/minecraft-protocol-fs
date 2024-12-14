namespace SandBoxLib;

public class MinecraftProtocol
{
    public UseItemSender SendUseItem { get; }

    public IObservable<Packet> OnPacket { get; }

    public IObservable<ChatMessagePacket> OnChatMessage { get; }
}

public abstract class Packet;

public class ChatMessagePacket : Packet
{
    public string Message { get; }

    public sealed class _340_765 : ChatMessagePacket
    {
        public int SequenceId { get; }
    }
}