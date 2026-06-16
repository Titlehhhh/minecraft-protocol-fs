using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace McpServer.Services;

public sealed record RagOptions(
    string? EmbeddingBaseUrl,
    string? EmbeddingModel,
    string? QdrantUrl,
    string Collection)
{
    public bool EmbeddingConfigured =>
        !string.IsNullOrWhiteSpace(EmbeddingBaseUrl) &&
        !string.IsNullOrWhiteSpace(EmbeddingModel);

    public bool QdrantConfigured => !string.IsNullOrWhiteSpace(QdrantUrl);

    public bool Enabled => EmbeddingConfigured && QdrantConfigured;

    public string[] Missing
    {
        get
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(EmbeddingBaseUrl)) missing.Add("RAG_EMBEDDING_BASE_URL");
            if (string.IsNullOrWhiteSpace(EmbeddingModel)) missing.Add("RAG_EMBEDDING_MODEL");
            if (string.IsNullOrWhiteSpace(QdrantUrl)) missing.Add("RAG_QDRANT_URL");
            return missing.ToArray();
        }
    }

    public static RagOptions FromConfiguration(IConfiguration configuration)
    {
        var embeddingBaseUrl =
            configuration["Rag:EmbeddingBaseUrl"] ??
            configuration["RAG_EMBEDDING_BASE_URL"] ??
            Environment.GetEnvironmentVariable("RAG_EMBEDDING_BASE_URL") ??
            Environment.GetEnvironmentVariable("EMBEDDING_BASE_URL");

        var embeddingModel =
            configuration["Rag:EmbeddingModel"] ??
            configuration["RAG_EMBEDDING_MODEL"] ??
            Environment.GetEnvironmentVariable("RAG_EMBEDDING_MODEL") ??
            Environment.GetEnvironmentVariable("EMBEDDING_MODEL");

        var qdrantUrl =
            configuration["Rag:QdrantUrl"] ??
            configuration["RAG_QDRANT_URL"] ??
            Environment.GetEnvironmentVariable("RAG_QDRANT_URL") ??
            Environment.GetEnvironmentVariable("QDRANT_URL");

        var collection =
            configuration["Rag:QdrantCollection"] ??
            configuration["RAG_QDRANT_COLLECTION"] ??
            Environment.GetEnvironmentVariable("RAG_QDRANT_COLLECTION") ??
            "mcprotonet_protocol_chunks";

        return new RagOptions(
            NormalizeBaseUrl(embeddingBaseUrl),
            embeddingModel,
            NormalizeBaseUrl(qdrantUrl),
            collection);
    }

    private static string? NormalizeBaseUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');
}
