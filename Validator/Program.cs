using System.Text.Json;
using Protodef;
using Protodef.Converters;
using Protodef.Enumerable;

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