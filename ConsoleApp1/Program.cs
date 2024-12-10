using System;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
    }
}

//Это я сам создам
public interface IPacketSender
{
    ValueTask SendPacket(MemoryOwner<byte> getWrittenMemory);
}


