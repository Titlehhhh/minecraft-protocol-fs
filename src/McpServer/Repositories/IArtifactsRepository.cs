using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Repositories;

public interface IArtifactsRepository
{
    Task<ArtifactInfo> SaveTextAsync(
        string fileName,
        string content,
        string contentType = "text/plain; charset=utf-8",
        CancellationToken cancellationToken = default);

    Task<ArtifactInfo> SaveBytesAsync(
        string fileName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<ArtifactInfo?> GetInfoAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string artifactId,
        CancellationToken cancellationToken = default);
}