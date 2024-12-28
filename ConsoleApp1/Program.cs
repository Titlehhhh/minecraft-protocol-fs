using SandBoxLib;

namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        RunBot();
    }

    static async Task RunBot()
    {
        var packet = new ClientUseItemPacket._340_567(5, 6);

        var p2 = new ClientChatPacket("asd");


        MinecraftProtocol protocol = new MinecraftProtocol();
    
        // Мульти версионная отправка
        await protocol.SendUseItem.Send(hand: 0);
        
        // Определенная версия
        if(protocol.SendUseItem.TrySend_759_766(out var sender))
        {
            await sender.Send(hand: 0, sequence: 0);
        }
        

        var observable = protocol.OnPacket<ChatMessagePacket>();
    }
}