using MinecraftData;
using Protodef;
using Protodef.Enumerable;

namespace PacketGenerator.Core;

public static class ProtocolValidator
{
    public static void Validate(ProtodefProtocol protocol, ProtocolInfo info)
    {
        foreach (var ns in protocol.EnumerateNamespaces())
        {
            var relativePath = info.Path.RelativeTo(MinecraftPaths.DataPath);
            try
            {
                var packets = ns.Types.Keys.Where(x => x.StartsWith("packet_"));

                var container = ns.Types["packet"] as ProtodefContainer;


                var mapper = container["params"] as ProtodefSwitch;

                foreach (var packet in packets)
                {
                    if (!Contains(mapper, packet))
                    {
                        throw new Exception(
                            $"Packet {packet} does not contain in protocol {relativePath}");
                    }
                }

                var ids = container["name"] as ProtodefMapper;
                foreach (var (packetId, packetName) in ids.Mappings)
                {
                    if (!mapper.Fields.ContainsKey(packetName))
                    {
                        throw new Exception($"Packet {packetName} for id {packetId} does not contain in protocol {relativePath}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in namespace {ns.Fullname} path {relativePath}");
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }

    private static bool Contains(ProtodefSwitch sw, string name)
    {
        return sw.Fields.Values
            .Select(x=>x.ToString())
            .Any(x => x == name);
    }
}