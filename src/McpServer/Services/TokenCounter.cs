using System;
using Microsoft.ML.Tokenizers;

namespace McpServer.Services;

public static class TokenCounter
{
    // cl100k_base (GPT-4 / Claude encoding) — loaded once, ~12MB, lives for app lifetime
    private static readonly Lazy<TiktokenTokenizer> _tokenizer =
        new(() => TiktokenTokenizer.CreateForModel("gpt-4"), isThreadSafe: true);

    public static int Count(string text) =>
        _tokenizer.Value.CountTokens(text);

    public static int Count(string system, string user) =>
        Count(system) + Count(user);
}
