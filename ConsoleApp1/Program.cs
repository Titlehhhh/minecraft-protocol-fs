using SandBoxLib;

namespace ConsoleApp1;



public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
    async Task RunBot()
    {
        var packet = new ClientUseItemPacket._340_567(5, 6);

        var p2 = new ClientChatPacket("asd");
        

        MinecraftProtocol protocol = new MinecraftProtocol();

        
        
        if (protocol.SendUseItem.TrySend_759_766(out var sender))
        {
            await sender.Send(5, 6);
        }

        await protocol.SendUseItem.Send(5);

        protocol.OnPacket<ChatMessagePacket._340_765>()
            .Subscribe(packet => { Console.WriteLine(packet.SequenceId); });
    }
}

public class CustomPacket : Packet1
{
    private void G()
    {
        
    }
}

