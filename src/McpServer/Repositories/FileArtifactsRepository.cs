using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Repositories;

public sealed class FileArtifactsRepository : IArtifactsRepository
{
    private readonly string _rootPath;
    private readonly ConcurrentDictionary<string, ArtifactInfo> _index = new();

    public FileArtifactsRepository(ArtifactsOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        _rootPath = Path.GetFullPath(options.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ArtifactInfo> SaveTextAsync(
        string fileName,
        string content,
        string contentType = "text/plain; charset=utf-8",
        CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return await SaveBytesAsync(fileName, bytes, contentType, cancellationToken);
    }

    public async Task<ArtifactInfo> SaveBytesAsync(
        string fileName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var id = GenerateId();
        var safeFileName = SanitizeFileName(fileName);
        var artifactPath = GetArtifactPath(id);

        await File.WriteAllBytesAsync(artifactPath, content, cancellationToken);

        var info = new ArtifactInfo(
            Id: id,
            FileName: safeFileName,
            ContentType: string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType,
            Size: content.LongLength,
            CreatedAtUtc: DateTimeOffset.UtcNow
        );

        _index[id] = info;
        return info;
    }

    public Task<ArtifactInfo?> GetInfoAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        _index.TryGetValue(artifactId, out var info);
        return Task.FromResult<ArtifactInfo?>(info);
    }

    public Task<Stream?> OpenReadAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        var path = GetArtifactPath(artifactId);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.Open(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return Task.FromResult<Stream?>(stream);
    }

    // -----------------------
    // Helpers
    // -----------------------

    private string GetArtifactPath(string id)
        => Path.Combine(_rootPath, id);

    private static string GenerateId()
    {
        // URL-safe, короткий, без padding
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "artifact.bin";

        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        return fileName;
    }
}