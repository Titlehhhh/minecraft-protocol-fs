# PacketGenerator

PacketGenerator is a public toolset for inspecting Minecraft protocol packet definitions and
generating C# packet code for the McProtoNet ecosystem.

It uses `PrismarineJS/minecraft-data` as the upstream protocol dataset, builds a versioned
protocol data model, and exposes it through:

- a reusable protocol access library,
- a scriptable CLI,
- a stdio MCP server,
- a Web UI / REST API / HTTP MCP server,
- LLM-based generation workflows.

## Requirements

- .NET 11 SDK preview selected by `global.json`.
- Initialized `minecraft-data` submodule.
- OpenRouter API key only for LLM generation. Read-only CLI, REST, and MCP access do not
  require a key.

```powershell
git submodule update --init
dotnet build PacketGenerator.slnx -maxcpucount:1
```

## Quick Start

Use the CLI for packet and type inspection:

```powershell
tools\packetgen.cmd stats --format json
tools\packetgen.cmd packets --filter keep_alive --format json
tools\packetgen.cmd packet play.toClient.keep_alive --format toon
tools\packetgen.cmd type ArmorTrimMaterial --format json
tools\packetgen.cmd composition play.toClient.keep_alive --format json
tools\packetgen.cmd graph --ns play --direction toClient --format json
tools\packetgen.cmd chunks --kind type --filter command_node --format json
```

Run the stdio MCP server:

```powershell
tools\packetgen-mcp.cmd
```

Run the Web UI, REST API, and HTTP MCP server:

```powershell
dotnet build src/McpServer/McpServer.csproj -maxcpucount:1 -p:BuildClientApp=false
dotnet run --project src/McpServer/McpServer.csproj --no-build
```

The server listens on `http://localhost:5000` by default. Override the port with `--port`,
`PORT`, or `MCP_SERVER_PORT`.

## Architecture

```text
minecraft-data protocol.json
-> ProtoDef parsing and validation
-> versioned type histories
-> packet/type repository
-> query and serialization services
-> CLI / stdio MCP / REST / HTTP MCP / generation / graph
```

### Projects

- `src/MinecraftData` resolves paths to the vendored `minecraft-data` dataset.
- `src/Protodef` models and serializes ProtoDef nodes.
- `src/ProtoCore` loads and validates versioned protocol files.
- `src/PacketGenerator.Protocol` is the protocol access layer.
- `src/PacketGenerator.Cli` is the normal stdout CLI.
- `src/PacketGenerator.McpStdio` is the stdio MCP server.
- `src/McpServer` hosts Web UI, REST, HTTP MCP, and LLM generation.

`PacketGenerator.Protocol` is the shared source for packet/type queries. Other surfaces should
stay thin over it instead of duplicating protocol parsing.

## CLI

```text
packets [--filter text] [--format json|toon]
types [--filter text] [--format json|toon]
native-types [--format json|toon]
types-by-kind [--format json|toon]
packet <packet-id> [--format json|toon]
type <type-id> [--format json|toon]
composition <packet-id> [--format json|toon]
chunks [--kind all|packet|type] [--filter text] [--max-chars N] [--format json|toon]
stats [--format json|toon]
graph [--ns play] [--direction toClient] [--include-types false] [--format json|toon]
```

Exit codes:

```text
0 ok
1 unexpected error
2 invalid args
3 not found
4 load error
5 invalid format
```

## Stdio MCP

`tools\packetgen-mcp.cmd` starts a stdio MCP server with read-only tools:

```text
list_packets
list_types
list_native_types
list_types_by_kind
get_packet_schema
get_type_schema
get_packet_composition
get_protocol_stats
get_protocol_graph
```

## REST / HTTP MCP

`src/McpServer` exposes:

```text
GET  /api/packets
GET  /api/packets/{ns}/{dir}
GET  /api/stats
GET  /api/types
GET  /api/native-types
GET  /api/types-by-kind
GET  /api/composition/{id}
GET  /api/schema/{id}
GET  /api/type/{id}
GET  /api/graph
GET  /api/chunks/status
GET  /api/chunks?kind=all|packet|type&filter=text&maxChars=900
GET  /api/chunks/{id}?kind=packet|type&maxChars=900
POST /api/chunks/index
POST /api/chunks/search
POST /api/prompt
POST /api/generate
POST /api/generate/batch
```

The Web UI has a `Chunks` view for inspecting deterministic protocol chunks. The chunk
viewer is always available. Vector indexing and semantic search are enabled only when these
variables are configured:

```powershell
$env:RAG_EMBEDDING_BASE_URL="http://127.0.0.1:1234/v1"
$env:RAG_EMBEDDING_MODEL="text-embedding-mxbai-embed-large-v1"
$env:RAG_QDRANT_URL="http://127.0.0.1:6333"
$env:RAG_QDRANT_COLLECTION="mcprotonet_protocol_chunks"
```

HTTP MCP is available at:

```text
http://localhost:5000/mcp
```

## OpenRouter Configuration

Read-only access does not require an API key. For generation:

```powershell
cd src/McpServer
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-..."
```

or set:

```powershell
$env:OPENROUTER_API_KEY="sk-or-..."
```

## Agent Notes

For packet examples, history, composition, graph, or edge-case discovery, use the CLI, stdio
MCP, or REST APIs. Raw `minecraft-data` files are the input dataset, not the normal inspection
surface.

See `AGENTS.md` and `AI_CONTEXT.md` for detailed agent guidance.
