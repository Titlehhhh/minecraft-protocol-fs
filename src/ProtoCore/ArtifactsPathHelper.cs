using PacketGenerator.Constants;
using TruePath;

namespace ProtoCore;

public static class ArtifactsPathHelper
{
    public static readonly AbsolutePath ArtifactsPath = 
        AbsolutePath.Create(ArtifactsPaths.ArtifactsDir);
}