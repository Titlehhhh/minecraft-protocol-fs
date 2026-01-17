namespace McpServer.Repositories;

public sealed class ArtifactsOptions
{
    /// <summary>
    /// Root directory where artifacts are stored.
    /// Should be a persistent volume in Docker.
    /// </summary>
    public string RootPath { get; init; } = "artifacts";
}