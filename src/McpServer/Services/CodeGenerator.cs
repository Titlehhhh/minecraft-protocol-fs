using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Repositories;
using Protodef;
using Scriban;
using Toon.Format;
using TruePath;
using TruePath.SystemIo;

namespace McpServer.Services;

public class CodeGenerator
{
    private readonly IProtocolRepository _repository;

    public CodeGenerator(IProtocolRepository repository)
    {
        _repository = repository;
    }

    public async Task<(string System, string User, PacketDefinition Packet)> BuildPromptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var supported = _repository.GetSupportedProtocols();
        var first = supported.From.ToString();
        var last = supported.To.ToString();

        var packet = _repository.GetPacket(id);

        var json = JsonSerializer.SerializeToNode(packet.History, ProtodefType.DefaultJsonOptions)!;
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

        var system =
            await (promptsFolder / "SystemPrompt.md").ReadAllTextAsync(cancellationToken);

        var skeleton =
            await (promptsFolder / "Sceleton.md").ReadAllTextAsync(cancellationToken);

        var availableMethods =
            await (promptsFolder / "AvailableMethods.md").ReadAllTextAsync(cancellationToken);

        var basePrompt =
            await (promptsFolder / "BasePrompt.md").ReadAllTextAsync(cancellationToken);

        var lastPart = id.Split('.').Last();
        var withoutPrefix = lastPart.StartsWith("packet_", StringComparison.OrdinalIgnoreCase)
            ? lastPart["packet_".Length..]
            : lastPart;
        var className = string.Concat(
            withoutPrefix.Split('_').Select(w =>
                w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..])) + "Packet";

        var user = Template.ParseLiquid(basePrompt).Render(new
        {
            ClassName = className,
            Methods = availableMethods,
            Toon = toon,
            Skeleton = skeleton
        });

        return (system, user, packet);
    }
}