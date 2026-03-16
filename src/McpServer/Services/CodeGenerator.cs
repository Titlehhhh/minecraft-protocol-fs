using System;
using System.Diagnostics;
using System.IO;
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

        // Remove null version ranges — they mean "packet didn't exist in those versions".
        // LLM should only see actionable schema entries.
        var nullKeys = obj.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList();
        foreach (var k in nullKeys)
            obj.Remove(k);

        var promptsFolder = ResolvePromptsFolder();

        var system           = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "SystemPrompt.md"), cancellationToken);
        var skeleton         = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "Sceleton.md"), cancellationToken);
        var availableMethods = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "AvailableMethods.md"), cancellationToken);
        var basePrompt       = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "BasePrompt.md"), cancellationToken);

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

    /// <summary>
    /// Walks up from the assembly directory until it finds a folder containing "Prompts/CodeGeneration".
    /// Falls back to current working directory so hot-edit works during development.
    /// </summary>
    private static string ResolvePromptsFolder()
    {
        const string relative = "Prompts/CodeGeneration";
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, relative);
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        // Fallback to CWD (works when launched via dotnet run from project directory)
        return Path.Combine(Directory.GetCurrentDirectory(), relative);
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
        if (!string.IsNullOrEmpty(cfg.ReasoningEffort))
        {
#pragma warning disable OPENAI001
            var effort    = cfg.ReasoningEffort switch
            {
                "low"             => OpenAI.Chat.ChatReasoningEffortLevel.Low,
                "medium"          => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
                "high" or "xhigh" => OpenAI.Chat.ChatReasoningEffortLevel.High,
                _                 => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
            };
            var maxTokens = cfg.MaxOutputTokens;
            var temp      = cfg.Temperature;
            return new ChatOptions
            {
                MaxOutputTokens          = maxTokens,
                Temperature              = temp,
                RawRepresentationFactory = _ => new OpenAI.Chat.ChatCompletionOptions
                {
                    ReasoningEffortLevel = effort,
                    MaxOutputTokenCount  = maxTokens,
                    Temperature          = temp,
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
