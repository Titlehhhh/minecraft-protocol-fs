using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Dunet;

namespace SandBoxLib;

public interface IPacket<in T> where T : IPacket<T>
{
    internal static abstract void Read(T packet, IMinecraftPrimitiveReader reader, int protocolVersion);
    //internal void Write(IMinecraftPrimitiveWriter writer, int protocolVersion);
}



public static class PacketMarshaller
{
    public static void ReadPacket<T>(T packet, MinecraftPrimitiveReader reader, int protocolVersion)
        where T : IPacket<T>
    {
        T.Read(packet, reader, protocolVersion);
    }
}

public interface ITest
{
   internal void Read(IMinecraftPrimitiveReader reader, int protocolVersion);
}

public static class Marshaller
{
    public static void ReadPacket<T>(T packet, MinecraftPrimitiveReader reader, int protocolVersion)
        where T : ITest
    {
        packet.Read(reader, protocolVersion);
    }
}

public abstract class PacketTest : ITest
{
    public static PacketTest Create()
    {
        return new Impl();
    }
    private class Impl : PacketTest
    {
        protected override void ReadPacket(IMinecraftPrimitiveReader reader, int protocolVersion)
        {
            Console.WriteLine("Hi");
        }
    }
    
    protected abstract void ReadPacket(IMinecraftPrimitiveReader reader, int protocolVersion);
    
    void ITest.Read(IMinecraftPrimitiveReader reader, int protocolVersion)
    {
        ReadPacket(reader, protocolVersion);
    }
}


public abstract class PositionLookPacket : IPacket<PositionLookPacket>
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }


    public V765To767Fields? V765To767;
    public V768To769Fields? V768To769;


    public static PositionLookPacket Create()
    {
        return new Impl();
    }

    private class Impl : PositionLookPacket
    {
        public Impl()
        {
        }


        protected override void ReadPacket(IMinecraftPrimitiveReader reader, int protocolVersion)
        {
            if (protocolVersion is >= 765 and < 768)
                V765To767 = ReadV765To767Fields(reader, protocolVersion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static V765To767Fields ReadV765To767Fields(IMinecraftPrimitiveReader reader, int protocolVersion)
        {
            return new V765To767Fields
            {
                OnGround = reader.ReadBoolean()
            };
        }
    }


    public struct V765To767Fields
    {
        public bool OnGround { get; set; }
    }

    public struct V768To769Fields
    {
        public byte Flags { get; set; }
    }

    protected abstract void ReadPacket(IMinecraftPrimitiveReader reader, int protocolVersion);


    static void IPacket<PositionLookPacket>.Read(PositionLookPacket packet, IMinecraftPrimitiveReader reader,
        int protocolVersion)
    {
        packet.ReadPacket(reader, protocolVersion);
    }
}

public class Prog
{
    public static void Bot()
    {
        var packet = PositionLookPacket.Create();

        packet.Pitch = 5;
        packet.Yaw = 0;
        packet.X = 0;
        packet.Y = 0;
        packet.Z = 0;

        packet.V765To767 = new()
        {
            OnGround = true
        };
        packet.V768To769 = new()
        {
            Flags = 1
        };
    }
}