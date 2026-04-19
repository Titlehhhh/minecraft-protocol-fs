using System;
using System.Collections.Generic;
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
    private readonly IProtocolRepository  _repository;
    private readonly ModelConfigService   _modelConfig;
    private readonly IArtifactsRepository _artifacts;
    private readonly IComplexityAssessor  _assessor;

    public CodeGenerator(
        IProtocolRepository repository,
        ModelConfigService modelConfig,
        IArtifactsRepository artifacts,
        IComplexityAssessor assessor)
    {
        _repository  = repository;
        _modelConfig = modelConfig;
        _artifacts   = artifacts;
        _assessor    = assessor;
    }

    public async Task<GenerationData> GenerateAsync(string id, CancellationToken cancellationToken = default)
    {
        var (system, user, packet) = await BuildPromptAsync(id, cancellationToken);

        var systemTokenCount = TokenCounter.Count(system);
        var userTokenCount   = TokenCounter.Count(user);
        var tokenCount       = systemTokenCount + userTokenCount;
        var complexityScore  = PacketComplexityScorer.Compute(packet.History);
        var assessment       = await _assessor.AssessAsync(packet.History, cancellationToken);
        var (model, reasoningEffort, returnToClaude, endpoint) = _modelConfig.PickModel(assessment.Tier);

        var className = BuildClassName(id);

        if (returnToClaude)
            return new GenerationData
            {
                Name             = className,
                SystemTokenCount = systemTokenCount,
                UserTokenCount   = userTokenCount,
                TokenCount       = tokenCount,
                ComplexityScore  = complexityScore,
                Tier             = assessment.Tier.ToLabel(),
                AssessorScore    = assessment.LlmScore,
                AssessorReason   = assessment.Reason,
                SystemPrompt     = system,
                UserPrompt       = user,
            };

        var sw = Stopwatch.StartNew();

        using var client = _modelConfig.CreateClient(model, endpoint);

        ChatMessage[] messages =
        [
            new(ChatRole.System, system),
            new(ChatRole.User, user)
        ];

        var chatOptions = ModelConfigService.BuildChatOptions(_modelConfig.Config.MaxOutputTokens, _modelConfig.Config.Temperature, reasoningEffort, _modelConfig.Config.TopP, _modelConfig.Config.Seed);
        var response = await client.GetResponseAsync(messages, chatOptions, cancellationToken);

        sw.Stop();

        long? inputTokens = null, 
            cachedTokens = null, 
            outputTokens = null, 
            reasoningTokens = null, 
            totalTokens = null;
        if (response.Usage is { } usage)
        {
            inputTokens     = usage.InputTokenCount;
            outputTokens    = usage.OutputTokenCount;
            totalTokens     = usage.TotalTokenCount;
            cachedTokens    = usage.CachedInputTokenCount is > 0 ? usage.CachedInputTokenCount : null;
            reasoningTokens = usage.ReasoningTokenCount   is > 0 ? usage.ReasoningTokenCount   : null;
        }

        var rawCode = ExtractCode(response.Text);
        var code    = PacketPostProcessor.Process(rawCode, packet, _repository.GetSupportedProtocols());

        var artifact = await _artifacts.SaveTextAsync(
            className + ".cs",
            code,
            "text/plain; charset=utf-8",
            cancellationToken);

        return new GenerationData
        {
            Name             = className,
            Code             = code,
            Link             = $"/artifacts/{artifact.Id}",
            SystemTokenCount = systemTokenCount,
            UserTokenCount   = userTokenCount,
            TokenCount       = tokenCount,
            ComplexityScore  = complexityScore,
            Tier             = assessment.Tier.ToLabel(),
            AssessorScore    = assessment.LlmScore,
            AssessorReason   = assessment.Reason,
            ElapsedMs        = sw.ElapsedMilliseconds,
            Model            = model,
            InputTokens      = inputTokens,
            CachedTokens     = cachedTokens,
            OutputTokens     = outputTokens,
            ReasoningTokens  = reasoningTokens,
            TotalTokens      = totalTokens,
        };
    }

    public async Task<(string System, string User, PacketDefinition Packet)> BuildPromptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var supported = _repository.GetSupportedProtocols();
        var packet    = _repository.GetPacket(id);

        var resolvedHistory = packet.History
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value is { } t ? t.CreatePrimitiveResolvedCopy() : (ProtodefType?)null);

        var json = JsonSerializer.SerializeToNode(resolvedHistory, ProtodefType.DefaultJsonOptions)!;
        var obj  = json.AsObject();

        PacketPostProcessor.ApplyVersionAliases(obj, supported);

        // Remove null version ranges — they mean "packet didn't exist in those versions".
        // LLM should only see actionable schema entries.
        var nullKeys = obj.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList();
        foreach (var k in nullKeys)
            obj.Remove(k);

        var promptsFolder = ResolvePromptsFolder();

        var systemBase = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "SystemPrompt.md"), cancellationToken);
        var skeleton   = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "Sceleton.md"), cancellationToken);
        var basePrompt = await File.ReadAllTextAsync(Path.Combine(promptsFolder, "BasePrompt.md"), cancellationToken);

        var composition = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, type) in resolvedHistory)
            if (type is not null)
                composition.UnionWith(ProtodefTypeAnalyzer.GetTypeComposition(type));

        var sectionsFolder   = Path.Combine(promptsFolder, "Methods");
        var availableMethods = ContextBuilder.Build(composition, sectionsFolder, _modelConfig.Config.DynamicContext);

        var system = systemBase
            + "\n\n# TYPES AND IO METHODS\n\n" + availableMethods
            + "\n\n# STRUCTURE TEMPLATE\n\n" + skeleton;

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
            Schema       = schema,
            FormatHeader = formatHeader,
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

    private static string ExtractCode(string text)
    {
        var match = Regex.Match(text, @"```(?:csharp|cs)\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}
