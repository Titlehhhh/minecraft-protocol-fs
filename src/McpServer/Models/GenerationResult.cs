using System.Diagnostics.CodeAnalysis;

namespace McpServer.Models;

public sealed class GenerationResult
{
    public required string Id { get; init; }
    public GenerationData? Data { get; init; }
    public GenerationError? Error { get; init; }

    [MemberNotNullWhen(true,  nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Data is not null;

    public static GenerationResult Ok(string id, GenerationData data) =>
        new() { Id = id, Data = data };

    public static GenerationResult Fail(string id, GenerationError error) =>
        new() { Id = id, Error = error };
}
