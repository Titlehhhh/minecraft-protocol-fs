using SandBoxLib;
using System;

namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        ChatMessageServerPacket.VersionSupported(500);
        ChatMessageServerPacket.V1.VersionSupported(500);
        ChatMessageServerPacket.V2.VersionSupported(500);
    }
}