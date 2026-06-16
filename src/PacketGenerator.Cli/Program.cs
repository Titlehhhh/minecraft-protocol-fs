using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using PacketGenerator.Protocol.Graph;
using PacketGenerator.Protocol.Loading;
using PacketGenerator.Protocol.Queries;
using PacketGenerator.Protocol.Rag;
using PacketGenerator.Protocol.Serialization;

const int Ok = 0;
const int UnexpectedError = 1;
const int InvalidArgs = 2;
const int NotFound = 3;
const int LoadError = 4;
const int InvalidFormat = 5;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 0 || IsHelp(args[0]))
    {
        PrintUsage();
        return args.Length == 0 ? InvalidArgs : Ok;
    }

    if (!TryReadFormat(args, out var format, out var formatError))
    {
        Console.Error.WriteLine(formatError);
        return InvalidFormat;
    }

    try
    {
        var repository = await ProtocolDataLoader.LoadRepositoryAsync(new ProtocolDataOptions());
        var query = new ProtocolQueryService(repository);
        var usage = new ProtocolUsageQueries(repository);
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "packets":
                Write(query.GetPackets(ReadOption(args, "--filter")), format);
                return Ok;

            case "types":
                Write(query.GetTypes(ReadOption(args, "--filter")), format);
                return Ok;

            case "native-types":
                Write(query.GetNativeTypes(), format);
                return Ok;

            case "types-by-kind":
                Write(query.GetTypesByKind(), format);
                return Ok;

            case "usage":
                Write(usage.GetUsage(ReadIntOption(args, "--top"), ReadOption(args, "--kind")), format);
                return Ok;

            case "users":
                return WriteById(args, id => usage.GetUsers(id), format);

            case "deps":
                return WriteById(args, id => usage.GetDependencies(id), format);

            case "packet":
                return WriteSchema(args, id => query.GetPacketSchema(id, format), format);

            case "type":
                return WriteSchema(args, id => query.GetTypeSchema(id, format), format);

            case "composition":
                return WriteById(args, id => query.GetPacketComposition(id), format);

            case "chunks":
            {
                var maxChars = ReadIntOption(args, "--max-chars") ?? 900;
                var chunker = new ProtocolRagChunker(
                    repository,
                    options: new ProtocolRagChunkOptions(MaxTextChars: maxChars));
                Write(chunker.BuildChunks(ReadOption(args, "--kind") ?? "all", ReadOption(args, "--filter")), format);
                return Ok;
            }

            case "stats":
                Write(query.GetStats(), format);
                return Ok;

            case "graph":
            {
                var graph = new ProtocolGraphBuilder(repository).Build(
                    ReadOption(args, "--ns"),
                    ReadOption(args, "--direction"),
                    !string.Equals(ReadOption(args, "--include-types"), "false", StringComparison.OrdinalIgnoreCase));
                Write(graph, format);
                return Ok;
            }

            default:
                Console.Error.WriteLine($"Unknown command '{args[0]}'.");
                PrintUsage();
                return InvalidArgs;
        }
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return NotFound;
    }
    catch (KeyNotFoundException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return NotFound;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return LoadError;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        return UnexpectedError;
    }
}

static int WriteSchema(string[] args, Func<string, SchemaResult> load, OutputFormat format)
{
    var id = ReadId(args);
    if (id is null) return InvalidArgs;

    Write(load(id), format);
    return Ok;
}

static int WriteById<T>(string[] args, Func<string, T> load, OutputFormat format)
{
    var id = ReadId(args);
    if (id is null) return InvalidArgs;

    Write(load(id), format);
    return Ok;
}

static void Write<T>(T value, OutputFormat format)
{
    var json = JsonSerializer.SerializeToNode(value, JsonOptions()) ?? JsonValue.Create(value)!;
    var text = ProtocolSchemaSerializer.Serialize(json, format);
    Console.Out.WriteLine(text);
}

static JsonSerializerOptions JsonOptions()
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    foreach (var converter in Protodef.ProtodefType.DefaultJsonOptions.Converters)
        options.Converters.Add(converter);
    return options;
}

static bool TryReadFormat(string[] args, out OutputFormat format, out string? error)
{
    var raw = ReadOption(args, "--format") ?? ReadOption(args, "-f") ?? "json";
    if (OutputFormatParser.TryParse(raw, out format))
    {
        error = null;
        return true;
    }

    error = $"Invalid format '{raw}'. Valid values: json, toon.";
    return false;
}

static string? ReadId(string[] args)
{
    if (args.Length < 2 || args[1].StartsWith("-", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Command '{args[0]}' requires an id.");
        return null;
    }

    return args[1];
}

static string? ReadOption(string[] args, string name)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
        if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
            return "";
        return args[i + 1];
    }

    return null;
}

static int? ReadIntOption(string[] args, string name)
{
    var value = ReadOption(args, name);
    if (string.IsNullOrWhiteSpace(value)) return null;
    return int.TryParse(value, out var parsed) ? parsed : null;
}

static bool IsHelp(string value) =>
    value is "-h" or "--help" or "help";

static void PrintUsage()
{
    Console.Error.WriteLine("""
packetgen commands:
  packets [--filter text] [--format json|toon]
  types [--filter text] [--format json|toon]
  native-types [--format json|toon]
  types-by-kind [--format json|toon]
  usage [--top N] [--kind packet|type|shape|native] [--format json|toon]
  users <packet|type|native|shape-id> [--format json|toon]
  deps <packet|type-id> [--format json|toon]
  packet <packet-id> [--format json|toon]
  type <type-id> [--format json|toon]
  composition <packet-id> [--format json|toon]
  chunks [--kind all|packet|type] [--filter text] [--max-chars N] [--format json|toon]
  stats [--format json|toon]
  graph [--ns play] [--direction toClient] [--include-types false] [--format json|toon]

exit codes:
  0 ok, 1 unexpected error, 2 invalid args, 3 not found, 4 load error, 5 invalid format
""");
}
