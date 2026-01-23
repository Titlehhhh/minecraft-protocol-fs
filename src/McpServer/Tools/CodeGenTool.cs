using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Protodef;
using Toon.Format;

namespace McpServer.Tools;

[McpServerToolType]
public static class CodeGenTool
{
    [McpServerTool(
         Name = "generate_packet"
     ), Description("Generates a C# packet class from a Minecraft protocol packet identifier. " +
                    "The tool resolves the packet schema across protocol versions, " +
                    "runs code generation using sampling, saves the generated code as an artifact, " +
                    "and returns a link to download the resulting .cs file.")]
    public static async Task<GenerationResult> GeneratePacket(
        ModelContextProtocol.Server.McpServer thisServer,
        IArtifactsRepository artifacts,
        LinkGenerator linkGenerator,
        IConfiguration configuration,
        CodeGenerator codeGenerator,
        [Description(
            "Packet identifier to generate code for. " +
            "Example: 'play.toServer.packet_look' or 'play.toClient.packet_action_bar'."
        )]
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var test = await codeGenerator.GenerateCodeAsync(id, cancellationToken);
        }
        catch (Exception e)
        {
            throw new McpException($"Error: {e.Message}", e);
        }
        throw new McpException("Not implemented");
    }
}