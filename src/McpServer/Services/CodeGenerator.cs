using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using McpServer.Repositories;
using OpenAI;
using OpenAI.Chat;
using Protodef;
using Scriban;
using Toon.Format;
using TruePath;
using TruePath.SystemIo;

namespace McpServer.Services;

public class CodeGenerator
{
    private readonly OpenAIClient _openAIClient;
    private readonly IProtocolRepository _repository;

    public CodeGenerator(OpenAIClient openAIClient, IProtocolRepository repository)
    {
        _openAIClient = openAIClient;
        _repository = repository;
    }

    private static string Replace(
        string str,
        string first,
        string last)
    {
        return str.Replace(first, "first").Replace(last, "last");
    }

    public async Task<string> GenerateCodeAsync(string id, CancellationToken cancellationToken = default)
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

            var newKey = Replace(node.Key, first, last);
            if (newKey != node.Key)
            {
                obj.SetAt(i, newKey, node.Value?.DeepClone());
            }
        }

        var toon = ToonEncoder.EncodeNode(json, new ToonEncodeOptions());

        var promptsFolder = AbsolutePath.CurrentWorkingDirectory
                            / "Prompts" / "CodeGeneration";


        var system =
            await (promptsFolder / "SystemPrompt.md")
                .ReadAllTextAsync(cancellationToken);

        var sceleton =
            await (promptsFolder / "Sceleton.md")
                .ReadAllTextAsync(cancellationToken);

        var availableMethods =
            await (promptsFolder / "AvailableMethods.md")
                .ReadAllTextAsync(cancellationToken);

        var basePrompt =
            await (promptsFolder / "BasePrompt.md")
                .ReadAllTextAsync(cancellationToken);

        var csharp = Template.ParseLiquid(sceleton);


        var prompt = Template.ParseLiquid(basePrompt).Render(new
        {
            Methods = availableMethods,
            Toon = toon,
            Skeleton = sceleton
        });

        var start = Stopwatch.GetTimestamp();

        var gg = new StringBuilder();

        gg.AppendLine("System: ");
        gg.AppendLine(system);
        gg.AppendLine("User: ");
        gg.AppendLine(prompt);

        string text = gg.ToString();

        var code = await GenerateCodeAsync(system, prompt, cancellationToken);

        var time = Stopwatch.GetElapsedTime(start);

        Console.WriteLine($"Gen time {time.TotalSeconds} seconds");
        Console.WriteLine("Code: ");
        Console.WriteLine(code);
        return "HH";
    }

    private async Task<string> GenerateCodeAsync(
        string system,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var client = _openAIClient.GetChatClient("openai/gpt-oss-20b");

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(system),
            new UserChatMessage(prompt),
        };


        var result = await client.CompleteChatAsync(messages, new ChatCompletionOptions()
        {
            Temperature = 0f,
            TopP = 1.0f,
            ToolChoice = ChatToolChoice.CreateNoneChoice(),
            MaxOutputTokenCount = 4096
        }, cancellationToken);

        return result.Value.Content[0].Text;
    }
}