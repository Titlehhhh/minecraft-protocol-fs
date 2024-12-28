using SandBoxLib;

namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        var id = PacketIdHelper.GetPacketId(754,ClientPacket.UseItem);
        Console.WriteLine(id);
    }

    
}