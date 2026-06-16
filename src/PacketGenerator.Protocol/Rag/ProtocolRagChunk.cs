using System.Collections.Generic;

namespace PacketGenerator.Protocol.Rag;

public sealed record ProtocolRagChunk(
    string Id,
    string OwnerKind,
    string OwnerId,
    string ChunkKind,
    string Path,
    string VersionRange,
    string Text,
    string[] Fields,
    string[] Kinds,
    string[] Categories,
    string[] SemanticHints,
    string RawPath,
    int TextCharCount,
    int EstimatedTokenCount,
    string ContentHash);

public sealed record ProtocolRagChunkOptions(
    int MaxTextChars = 900,
    int MaxBodyChars = 360,
    int MaxListItems = 16,
    int MaxEstimatedTokens = 350,
    int FieldBatchSize = 6,
    int CaseBatchSize = 16)
{
    public int EffectiveMaxTextChars => MaxTextChars > 0 ? MaxTextChars : 900;
    public int EffectiveMaxBodyChars => MaxBodyChars > 0 ? MaxBodyChars : 360;
    public int EffectiveMaxListItems => MaxListItems > 0 ? MaxListItems : 16;
    public int EffectiveMaxEstimatedTokens => MaxEstimatedTokens > 0 ? MaxEstimatedTokens : 350;
    public int EffectiveFieldBatchSize => FieldBatchSize > 0 ? FieldBatchSize : 6;
    public int EffectiveCaseBatchSize => CaseBatchSize > 0 ? CaseBatchSize : 16;
}

public sealed record ProtocolRagChunkSet(IReadOnlyCollection<ProtocolRagChunk> Chunks);
