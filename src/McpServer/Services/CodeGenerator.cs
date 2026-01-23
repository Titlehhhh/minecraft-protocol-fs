using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task<string> GenerateCodeAsync(string id, CancellationToken cancellationToken = default)
    {
        var history = _repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(history, ProtodefType.DefaultJsonOptions)!;
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

        sceleton = csharp.Render(new
        {
            PacketName = history.Name
        });


        var prompt = Template.ParseLiquid(basePrompt).Render(new
        {
            Methods = availableMethods,
            Toon = toon,
            Skeleton = sceleton
        });

        await GenerateCodeAsync(system, prompt, cancellationToken);

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

        Console.WriteLine("System:");
        Console.WriteLine(system);

        Console.WriteLine();
        Console.WriteLine("User:");
        Console.WriteLine(prompt);

        var result = await client.CompleteChatAsync(messages, new ChatCompletionOptions()
        {
            Temperature = 0f
        }, cancellationToken);

        Console.WriteLine("Code:");
        Console.WriteLine(result.Value.Content[0].Text);

        return "";
    }
}