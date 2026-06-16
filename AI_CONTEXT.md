# PacketGenerator AI Context

PacketGenerator turns PrismarineJS `minecraft-data` protocol definitions into a queryable
protocol data model, then exposes that model through CLI, MCP, REST, Web UI, graph, and LLM
generation surfaces.

The important mental model is:

```text
minecraft-data protocol.json
-> ProtoDef parsing and validation
-> versioned type histories
-> packet/type repository
-> query and serialization services
-> CLI / stdio MCP / REST / HTTP MCP / generation / graph
```

## Protocol Data Flow

`minecraft-data` is the upstream data source. The Java protocol files are large generated
ProtoDef JSON documents. Agents should usually not inspect them directly.

`ProtoCore` loads the versioned `protocol.json` files selected by `minecraft-data/dataPaths.json`.
`Protodef` parses the JSON representation into typed C# nodes such as containers, switches,
mappers, arrays, options, buffers, bitfields, and primitive/custom types.

`PacketGenerator.Protocol` builds the usable protocol access layer:

- `ProtocolDataLoader` loads a configured protocol range.
- `HistoryBuilder` builds versioned histories and collapses adjacent identical structures.
- `ProtocolRepository` separates packet mappers from named protocol types.
- `ProtocolQueryService` exposes packet/type/schema/composition/stats queries.
- `ProtocolGraphBuilder` builds graph nodes and edges over packets, named types, native types,
  and ProtoDef shape nodes.
- `ProtocolRagChunker` emits deterministic structural chunks for embedding/search ingestion.
- `ProtocolSchemaSerializer` emits JSON or TOON.

## Packet And Type Separation

Packets are not just files or root JSON entries. They are derived from packet mapper types.

`ProtocolRepository` finds type histories whose id ends with `packet`. Each packet mapper is
expected to contain:

- `name`: a `mapper` from protocol packet id to packet name.
- `params`: a `switch` from packet name to packet payload type.

Packet payloads may be `void` or may point at named types. Type resolution is namespace-aware:
the repository tries local namespace names first and then shared/global names. This is why
manual raw-JSON inspection is easy to get wrong.

## Public Agent Entry Points

For read-only agent work, prefer the CLI:

```powershell
tools\packetgen.cmd stats --format json
tools\packetgen.cmd packets --filter "player|move" --format json
tools\packetgen.cmd packet play.toClient.keep_alive --format toon
tools\packetgen.cmd type ArmorTrimMaterial --format json
tools\packetgen.cmd composition play.toClient.keep_alive --format json
tools\packetgen.cmd graph --ns play --direction toClient --format json
tools\packetgen.cmd chunks --kind type --filter command_node --format json
```

For local MCP clients, use:

```powershell
tools\packetgen-mcp.cmd
```

For browser, REST, HTTP MCP, and generation workflows, run `src/McpServer`.

Read-only access does not require an OpenRouter key. Generation workflows do.

## Current CLI Commands

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

## Current Stdio MCP Tools

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

## REST And HTTP MCP

`McpServer` exposes read-only protocol endpoints, generation endpoints, a Web UI, and HTTP MCP
transport. The read-only endpoints are useful when another process is already running the
server:

```text
GET /api/packets
GET /api/packets/{ns}/{dir}
GET /api/stats
GET /api/types
GET /api/native-types
GET /api/types-by-kind
GET /api/composition/{id}
GET /api/schema/{id}
GET /api/type/{id}
GET /api/graph
GET /api/chunks/status
GET /api/chunks?kind=all|packet|type&filter=text&maxChars=900
GET /api/chunks/{id}?kind=packet|type&maxChars=900
POST /api/chunks/index
POST /api/chunks/search
POST /api/prompt
POST /api/generate
```

Chunk viewing works without external services. Vector indexing and semantic search are enabled
only when all RAG variables are configured:

```text
RAG_EMBEDDING_BASE_URL=http://127.0.0.1:1234/v1
RAG_EMBEDDING_MODEL=text-embedding-mxbai-embed-large-v1
RAG_QDRANT_URL=http://127.0.0.1:6333
RAG_QDRANT_COLLECTION=mcprotonet_protocol_chunks
```

## Known Design Boundaries

- The protocol access layer is the shared source for CLI, MCP, REST, graph, and generation.
- CLI and MCP wrappers should remain thin.
- Raw `minecraft-data` should not be used as packet examples unless the loader/parser itself
  is under investigation.
- File-based `tools/packetgen.cs` and `tools/packetgen-mcp.cs` are prototypes. The `.cmd`
  wrappers are the recommended agent entrypoints because they keep stdout/MCP framing clean.
- Avoid internal shorthand for the protocol access layer in public prose. Prefer
  `protocol core`, `protocol access layer`, or `protocol data model`.
