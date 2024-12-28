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
        var id = PacketIdHelper.GetPacketId(340,ClientPacket.UseItem);
    }
}