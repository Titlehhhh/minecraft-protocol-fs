using System.IO;

namespace McpServer.Helpers;

public static class PacketFileHelper
{
    public static string ResolveSubdir(string id)
    {
        var parts = id.Split('.');
        if (parts.Length < 2) return "";

        var nsName = parts[0].ToLowerInvariant() switch
        {
            "play"          => "Play",
            "login"         => "Login",
            "status"        => "Status",
            "configuration" => "Configuration",
            "handshaking"   => "Handshaking",
            var other       => other,
        };

        var dirName = parts[1].ToLowerInvariant() switch
        {
            "toclient" => "Clientbound",
            "toserver" => "Serverbound",
            var other  => other,
        };

        return Path.Combine(nsName, dirName);
    }
}
