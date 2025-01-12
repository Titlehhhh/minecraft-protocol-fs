using SandBoxLib;
using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;


namespace ConsoleApp1
{



    public class Program
    {


        public static void Main(string[] args)
        {
            TestPacket ggg = new TestPacket();
            M(ggg);
            IGGG baseG = new TestPacket();
            M(baseG);
        }

        public static void M<T>(T t) where T : IGGG
        {
            Console.WriteLine(T.Id);
            Console.WriteLine(t.GetId());
        }
    }

    public interface IGGG
    {
        public static virtual string Id { get; }
        public string GetId();
        
        
        public static virtual bool VersionSupported(int protocolVersion)
        {
            throw new NotImplementedException();
        }
    }
    public class TestPacket :IGGG
    {
        public static string Id => "asdasdasdasd";
        public string GetId()
        {
            return Id;
        }

        public static bool VersionSupported(int protocolVersion)
        {
            throw new NotImplementedException();
        }
    }
}