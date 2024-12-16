using SandBoxLib;

namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        RunBot();
    }

    static void RunBot()
    {
        var packet = new ClientUseItemPacket._340_567(5, 6);

        var p2 = new ClientChatPacket("asd");


        MinecraftProtocol protocol = new MinecraftProtocol();

        var observable = protocol.OnPacket<ChatMessagePacket>();
    }
}