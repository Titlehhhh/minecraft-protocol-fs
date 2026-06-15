namespace PacketGenerator.Protocol.Serialization;

public enum OutputFormat
{
    Json,
    Toon
}

public static class OutputFormatParser
{
    public static bool TryParse(string? value, out OutputFormat format)
    {
        switch ((value ?? "json").Trim().ToLowerInvariant())
        {
            case "json":
                format = OutputFormat.Json;
                return true;
            case "toon":
                format = OutputFormat.Toon;
                return true;
            default:
                format = default;
                return false;
        }
    }
}
