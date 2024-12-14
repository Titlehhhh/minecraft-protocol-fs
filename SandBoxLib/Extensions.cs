using System.Reactive.Linq;

namespace SandBoxLib;

public static class Extensions
{
    public static IObservable<T> OnPacket<T>(this MinecraftProtocol protocol) where T : Packet
    {
        return protocol.OnPacket.Where(x=> x is T).Cast<T>();
    }
}

