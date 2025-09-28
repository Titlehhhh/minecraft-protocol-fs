using TruePath;

namespace Validator;

public class ProtocolInfo(
    int Protocol,
    AbsolutePath Path,
    SortedSet<string> MinecraftVersions
)
{
    public int Protocol { get; init; } = Protocol;
    public AbsolutePath Path { get; init; } = Path;
    public SortedSet<string> MinecraftVersions { get; init; } = MinecraftVersions;

    public void Deconstruct(out int Protocol, out AbsolutePath Path, out SortedSet<string> MinecraftVersions)
    {
        Protocol = this.Protocol;
        Path = this.Path;
        MinecraftVersions = this.MinecraftVersions;
    }
}