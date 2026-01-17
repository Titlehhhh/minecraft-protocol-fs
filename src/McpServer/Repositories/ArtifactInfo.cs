using System;

namespace McpServer.Repositories;

public sealed record ArtifactInfo(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAtUtc
);