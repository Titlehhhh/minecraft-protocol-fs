using Protodef;
using TruePath;

namespace Validator;

public class ProtocolInfo(
    int version,
    AbsolutePath path,
    ProtodefProtocol? protocol,
    SortedSet<string> MinecraftVersions
)
{
    public int Version { get; init; } = version;
    public AbsolutePath Path { get; init; } = path;
    public ProtodefProtocol? Protocol { get; set; } = protocol;
    public SortedSet<string> MinecraftVersions { get; init; } = MinecraftVersions;
    
}