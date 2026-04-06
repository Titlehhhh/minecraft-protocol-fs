using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using Microsoft.Extensions.AI;
using Protodef;
using System.ClientModel.Primitives;
using OaiChat = OpenAI.Chat;

namespace McpServer.Services;

/// <summary>
/// LLM-based assessor using structured output (response_format: json_schema).
/// The model reasons freely, then emits a JSON object matching the schema.
/// Supports LM Studio (local) and OpenRouter. Falls back to structural on error.
/// </summary>
public sealed class LlmComplexityAssessor : IComplexityAssessor
{
    private readonly ModelConfigService _config;
    private readonly StructuralComplexityAssessor _fallback;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Structured output schema
    private const string AssessmentSchemaJson =
        """
        {
          "type": "object",
          "properties": {
            "tier":   { "type": "string", "enum": ["tiny", "easy", "medium", "heavy"] },
            "score":  { "type": "integer", "minimum": 0, "maximum": 100 },
            "reason": { "type": "string" }
          },
          "required": ["tier", "score", "reason"],
          "additionalProperties": false
        }
        """;

    private const string SystemPrompt =
        """
        You are a Minecraft packet implementation complexity assessor.
        Classify the packet schema into one of four tiers:

          tiny   — 1–3 primitive fields (varint, string, bool, i64, f32…), single version range, no arrays, no switch
          easy   — up to ~8 fields, mild version variation, basic types only
          medium — arrays, switch/case branching, nested containers, or several version ranges with type differences
          heavy  — deeply nested, many version conflicts, 2D arrays, complex multi-version switch trees

        Also provide a numeric score 0–100 (0 = trivial, 100 = extremely complex).
        """;

    private sealed record AssessmentDto(
        [property: JsonPropertyName("tier")]   string? Tier,
        [property: JsonPropertyName("score")]  int     Score,
        [property: JsonPropertyName("reason")] string? Reason
    );

    public LlmComplexityAssessor(ModelConfigService config, StructuralComplexityAssessor fallback)
    {
        _config   = config;
        _fallback = fallback;
    }

    public async ValueTask<ComplexityAssessment> AssessAsync(
        Dictionary<ProtocolRange, ProtodefType?> history,
        CancellationToken ct = default)
    {
        var cfg = _config.Config.Assessor;
        if (!cfg.Enabled || string.IsNullOrEmpty(cfg.Model))
            return await _fallback.AssessAsync(history, ct);

        try
        {
            var schema = JsonSerializer.Serialize(history, ProtodefType.DefaultJsonOptions);

            List<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, $"Packet schema (protodef JSON):\n{schema}"),
            ];

            var options = BuildOptions(cfg);

            using var client = _config.CreateClient(cfg.Model, cfg.Endpoint);
            var response = await client.GetResponseAsync(messages, options, ct);

            // LM Studio thinking models put output in reasoning_content, content is empty.
            // Fall back to extracting from raw response in that case.
            var text = response.Text;
            if (string.IsNullOrWhiteSpace(text))
                text = TryExtractReasoningContent(response);

            if (string.IsNullOrWhiteSpace(text))
            {
                Console.Error.WriteLine("[LlmComplexityAssessor] Empty response, falling back.");
            }
            else
            {
                var dto  = JsonSerializer.Deserialize<AssessmentDto>(text, JsonOpts);
                var tier = ParseTier(dto?.Tier);
                if (tier is not null && dto is not null)
                    return new ComplexityAssessment(tier.Value, dto.Score, dto.Reason);
                Console.Error.WriteLine($"[LlmComplexityAssessor] Could not parse: {text[..Math.Min(200, text.Length)]}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LlmComplexityAssessor] {ex.GetType().Name}: {ex.Message}");
        }

        return await _fallback.AssessAsync(history, ct);
    }

    private static ChatOptions BuildOptions(AssessorConfig cfg)
    {
#pragma warning disable OPENAI001
        var responseFormat = OaiChat.ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "complexity_assessment",
            jsonSchema:           BinaryData.FromString(AssessmentSchemaJson),
            jsonSchemaIsStrict:   true);

        if (!string.IsNullOrEmpty(cfg.ReasoningEffort))
        {
            var effort = cfg.ReasoningEffort switch
            {
                "low"    => OaiChat.ChatReasoningEffortLevel.Low,
                "medium" => OaiChat.ChatReasoningEffortLevel.Medium,
                _        => OaiChat.ChatReasoningEffortLevel.High,
            };
            return new ChatOptions
            {
                MaxOutputTokens          = cfg.MaxOutputTokens,
                Temperature              = 0f,
                RawRepresentationFactory = _ => new OaiChat.ChatCompletionOptions
                {
                    ResponseFormat       = responseFormat,
                    ReasoningEffortLevel = effort,
                    MaxOutputTokenCount  = cfg.MaxOutputTokens,
                    Temperature          = 0f,
                }
            };
        }

        return new ChatOptions
        {
            MaxOutputTokens          = cfg.MaxOutputTokens,
            Temperature              = 0f,
            RawRepresentationFactory = _ => new OaiChat.ChatCompletionOptions
            {
                ResponseFormat      = responseFormat,
                MaxOutputTokenCount = cfg.MaxOutputTokens,
            }
        };
#pragma warning restore OPENAI001
    }

    /// <summary>
    /// LM Studio thinking models put output in reasoning_content instead of content.
    /// Reads it from the raw OpenAI ChatCompletion via ModelReaderWriter.
    /// </summary>
    private static string? TryExtractReasoningContent(ChatResponse response)
    {
        try
        {
            if (response.RawRepresentation is not OaiChat.ChatCompletion completion) return null;
            var raw = ModelReaderWriter.Write(completion);
            using var doc = JsonDocument.Parse(raw);
            var msg = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");
            if (msg.TryGetProperty("reasoning_content", out var rc))
                return rc.GetString();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LlmComplexityAssessor] TryExtractReasoningContent failed: {ex.Message}");
        }
        return null;
    }

    private static ComplexityTier? ParseTier(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "tiny"   => ComplexityTier.Tiny,
        "easy"   => ComplexityTier.Easy,
        "medium" => ComplexityTier.Medium,
        "heavy"  => ComplexityTier.Heavy,
        _        => null,
    };
}
