using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Humanizer;
using Protodef;

namespace Validator;

class Program
{
    static void Main(string[] args)
    {
        var protocol = ProtodefProtocol.Deserialize(File.ReadAllText(
            "C:\\Users\\Title\\RiderProjects\\MinecraftDataFSharp\\minecraft-data\\data\\pc\\1.12.2\\protocol.json"));

        
        
        foreach (var ns in protocol.EnumerateNamespaces())
        {
            Console.WriteLine(ns.Fullname);
        }
    }
}