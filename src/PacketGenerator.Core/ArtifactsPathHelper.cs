using TruePath;

namespace PacketGenerator.Core;

public static class ArtifactsPathHelper
{
    public static readonly AbsolutePath ArtifactsPath = 
        AbsolutePath.Create(Constants.ArtifactsPaths.ArtifactsDir);
}