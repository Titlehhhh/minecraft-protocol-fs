using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using McpServer.Models;
using McpServer.Repositories;
using Microsoft.Extensions.AI;
using Protodef;
using Scriban;
using System.Text.Json;
using Toon.Format;
using TruePath;
using TruePath.SystemIo;

namespace McpServer.Services;

public class CodeGenerator
{
    private readonly IProtocolRepository _repository;
    private readonly ModelConfigService  _modelConfig;
    private readonly IArtifactsRepository _artifacts;

    public CodeGenerator(
        IProtocolRepository repository,
        ModelConfigService modelConfig,
        IArtifactsRepository artifacts)
    {
        _repository  = repository;
        _modelConfig = modelConfig;
        _artifacts   = artifacts;
    }

    public async Task<GenerationData> GenerateAsync(string id, CancellationToken cancellationToken = default)
    {
        var (system, user, packet) = await BuildPromptAsync(id, cancellationToken);

        var tokenCount = TokenCounter.Count(system, user);
        var (model, returnToClaude) = _modelConfig.PickModel(tokenCount);

        var className = BuildClassName(id);

        if (returnToClaude)
            return new GenerationData
            {
                Name         = className,
                TokenCount   = tokenCount,
                SystemPrompt = system,
                UserPrompt   = user,
            };

        var sw = Stopwatch.StartNew();

        using var client = _modelConfig.CreateClient(model);

        ChatMessage[] messages =
        [
            new(ChatRole.System, system),
            new(ChatRole.User, user)
        ];

        var chatOptions = BuildChatOptions(_modelConfig.Config);
        var response = await client.GetResponseAsync(messages, chatOptions, cancellationToken);

        sw.Stop();

        var rawCode = ExtractCode(response.Text);
        var code    = PacketPostProcessor.Process(rawCode, packet, _repository.GetSupportedProtocols());

        var artifact = await _artifacts.SaveTextAsync(
            className + ".cs",
            code,
            "text/plain; charset=utf-8",
            cancellationToken);

        return new GenerationData
        {
            Name       = className,
            Code       = code,
            Link       = $"/artifacts/{artifact.Id}",
            TokenCount = tokenCount,
            ElapsedMs  = sw.ElapsedMilliseconds,
            Model      = model,
        };
    }

    public async Task<(string System, string User, PacketDefinition Packet)> BuildPromptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var supported = _repository.GetSupportedProtocols();
        var first = supported.From.ToString();
        var last  = supported.To.ToString();

        var packet = _repository.GetPacket(id);

        var resolvedHistory = packet.History
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value is { } t ? t.CreatePrimitiveResolvedCopy() : (ProtodefType?)null);

        var json = JsonSerializer.SerializeToNode(resolvedHistory, ProtodefType.DefaultJsonOptions)!;
        var obj  = json.AsObject();

        for (var i = 0; i < obj.Count; i++)
        {
            var node   = obj.GetAt(i);
            var newKey = node.Key.Replace(first, "first").Replace(last, "last");
            if (newKey != node.Key)
                obj.SetAt(i, newKey, node.Value?.DeepClone());
        }

        var promptsFolder = AbsolutePath.CurrentWorkingDirectory / "Prompts" / "CodeGeneration";

        var system           = await (promptsFolder / "SystemPrompt.md").ReadAllTextAsync(cancellationToken);
        var skeleton         = await (promptsFolder / "Sceleton.md").ReadAllTextAsync(cancellationToken);
        var availableMethods = await (promptsFolder / "AvailableMethods.md").ReadAllTextAsync(cancellationToken);
        var basePrompt       = await (promptsFolder / "BasePrompt.md").ReadAllTextAsync(cancellationToken);

        var className = BuildClassName(id);

        string schema, formatHeader;
        var inputFormat = _modelConfig.Config.InputFormat;

        if (inputFormat == "json")
        {
            schema = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
            formatHeader =
                "JSON format — version history of this packet\n\n" +
                "> **IMPORTANT:** The schema uses protodef notation: `[\"<TYPE>\", [...fields...]]`.\n" +
                "> The first string element (e.g. `\"container\"`) is a **TYPE SPECIFIER**, NOT a field name.\n" +
                "> Only objects with `name` and `type` properties are actual packet fields.";
        }
        else
        {
            schema = ToonEncoder.EncodeNode(json, new ToonEncodeOptions());
            formatHeader =
                "Toon format — version history of this packet\n\n" +
                "> **IMPORTANT:** Lines starting with `- container` are **TYPE SPECIFIERS**, NOT field names.\n" +
                "> Only `fieldName,type` pairs after the fields marker are actual packet fields.";
        }

        var user = Template.ParseLiquid(basePrompt).Render(new
        {
            ClassName    = className,
            Methods      = availableMethods,
            Schema       = schema,
            FormatHeader = formatHeader,
            Skeleton     = skeleton,
        });

        return (system, user, packet);
    }

    public static string BuildClassName(string id)
    {
        var lastPart = id.Split('.').Last();
        var withoutPrefix = lastPart.StartsWith("packet_", StringComparison.OrdinalIgnoreCase)
            ? lastPart["packet_".Length..]
            : lastPart;
        return withoutPrefix.Pascalize() + "Packet";
    }

    private static ChatOptions BuildChatOptions(ModelConfig cfg)
    {
        // Reasoning mode: pass ReasoningEffortLevel via RawRepresentationFactory.
        // Temperature is intentionally not set — thinking models ignore or reject it.
        if (!string.IsNullOrEmpty(cfg.ReasoningEffort))
        {
#pragma warning disable OPENAI001
            var effort = cfg.ReasoningEffort switch
            {
                "low"             => OpenAI.Chat.ChatReasoningEffortLevel.Low,
                "medium"          => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
                "high" or "xhigh" => OpenAI.Chat.ChatReasoningEffortLevel.High,
                _                 => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
            };
            var maxTokens = cfg.MaxOutputTokens;
            return new ChatOptions
            {
                MaxOutputTokens        = maxTokens,
                RawRepresentationFactory = _ => new OpenAI.Chat.ChatCompletionOptions
                {
                    ReasoningEffortLevel = effort,
                    MaxOutputTokenCount  = maxTokens,
                }
            };
#pragma warning restore OPENAI001
        }

        return new ChatOptions
        {
            Temperature     = cfg.Temperature,
            MaxOutputTokens = cfg.MaxOutputTokens,
        };
    }

    private static string ExtractCode(string text)
    {
        var match = Regex.Match(text, @"```(?:csharp|cs)\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}
