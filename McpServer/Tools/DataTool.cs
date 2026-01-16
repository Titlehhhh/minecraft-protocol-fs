using System.ComponentModel;
using McpServer.Repositories;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DataTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string GetTypes(IProtocolRepository repository)
    {
        return string.Join(", ", repository.GetTypes());
    }
}

[McpServerResourceType]
public static class Resources
{
    
}