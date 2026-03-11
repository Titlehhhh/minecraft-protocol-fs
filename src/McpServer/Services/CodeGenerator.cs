using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using McpServer.Models;
using McpServer.Repositories;
using Microsoft.Extensions.AI;
using Protodef;
using Scriban;
using Toon.Format;
using TruePath;
using TruePath.SystemIo;

namespace McpServer.Services;

public class CodeGenerator
{
    private const int DirectGenerationThreshold = 4000;

    private readonly IProtocolRepository _repository;
    private readonly IChatClient _chatClient;
    private readonly IArtifactsRepository _artifacts;

    public CodeGenerator(
        IProtocolRepository repository,
        IChatClient chatClient,
        IArtifactsRepository artifacts)
    {
        _repository = repository;
        _chatClient = chatClient;
        _artifacts = artifacts;
    }

    public async Task<GenerationResult> GenerateAsync(string id, CancellationToken cancellationToken = default)
    {
        var (system, user, packet) = await BuildPromptAsync(id, cancellationToken);
        var tokenCount = 500;
            //TokenCounter.Count(system) + TokenCounter.Count(user);

        if (tokenCount > DirectGenerationThreshold)
            return new GenerationResult
            {
                TokenCount = tokenCount,
                SystemPrompt = system,
                UserPrompt = user
            };

        ChatMessage[] messages =
        [
            new(ChatRole.System, system),
            new(ChatRole.User, user)
        ];

        var response = await _chatClient.GetResponseAsync(messages, new ChatOptions
        {
            Temperature = 0f,
            MaxOutputTokens = 4096
        }, cancellationToken);

        var rawCode = ExtractCode(response.Text);
        var supportedRange = _repository.GetSupportedProtocols();
        var code = PacketPostProcessor.Process(rawCode, packet, supportedRange);
        var className = BuildClassName(id);

        var artifact = await _artifacts.SaveTextAsync(
            className + ".cs",
            code,
            "text/plain; charset=utf-8",
            cancellationToken);

        return new GenerationResult
        {
            Name = className,
            Link = $"/artifacts/{artifact.Id}",
            TokenCount = tokenCount
        };
    }

    public async Task<(string System, string User, PacketDefinition Packet)> BuildPromptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var supported = _repository.GetSupportedProtocols();
        var first = supported.From.ToString();
        var last = supported.To.ToString();

        var packet = _repository.GetPacket(id);

        // Resolve primitive alias types (container_id, angle, etc.) to real primitives
        // before serialization so LLM sees varint/u8/i16 instead of invented type names.
        var resolvedHistory = packet.History
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value is { } t ? t.CreatePrimitiveResolvedCopy() : (ProtodefType?)null);

        var json = JsonSerializer.SerializeToNode(resolvedHistory, ProtodefType.DefaultJsonOptions)!;
        var obj = json.AsObject();

        for (var i = 0; i < obj.Count; i++)
        {
            var node = obj.GetAt(i);
            var newKey = node.Key.Replace(first, "first").Replace(last, "last");
            if (newKey != node.Key)
                obj.SetAt(i, newKey, node.Value?.DeepClone());
        }

        var toon = ToonEncoder.EncodeNode(json, new ToonEncodeOptions());

        var promptsFolder = AbsolutePath.CurrentWorkingDirectory / "Prompts" / "CodeGeneration";

        var system = await (promptsFolder / "SystemPrompt.md").ReadAllTextAsync(cancellationToken);
        var skeleton = await (promptsFolder / "Sceleton.md").ReadAllTextAsync(cancellationToken);
        var availableMethods = await (promptsFolder / "AvailableMethods.md").ReadAllTextAsync(cancellationToken);
        var basePrompt = await (promptsFolder / "BasePrompt.md").ReadAllTextAsync(cancellationToken);

        var className = BuildClassName(id);

        var user = Template.ParseLiquid(basePrompt).Render(new
        {
            ClassName = className,
            Methods = availableMethods,
            Toon = toon,
            Skeleton = skeleton
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

    private static string ExtractCode(string text)
    {
        var match = Regex.Match(text, @"```(?:csharp|cs)\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}
