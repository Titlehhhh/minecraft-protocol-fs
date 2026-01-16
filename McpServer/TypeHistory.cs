using System.Collections.Generic;
using Protodef;

namespace McpServer;

public class TypeHistory
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<ProtocolRange, ProtodefType?> History { get; set; } = new();
}