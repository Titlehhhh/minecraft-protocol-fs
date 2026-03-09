using Microsoft.ML.Tokenizers;

namespace McpServer.Services;

public static class TokenCounter
{
    // cl100k_base — same encoding as GPT-4/Claude, good enough approximation
    private static Tokenizer? _tokenizer;

    private static Tokenizer GetTokenizer()
    {
        return _tokenizer ??= TiktokenTokenizer.CreateForModel("gpt-4");
        // cl100k_base, good Claude approximation
    }

    public static int Count(string text)
    {
        return GetTokenizer().CountTokens(text);
    }
}