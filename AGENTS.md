# PacketGenerator Agent Instructions

PacketGenerator is a public repository. Keep documentation and code comments self-contained:
do not reference local workspaces, private planning notes, sibling checkout paths, or personal
automation. Public docs may mention McProtoNet only as the target library/ecosystem.

## Read Order

Before changing code or doing protocol research, read:

1. `AI_CONTEXT.md`
2. The nearest nested `AGENTS.md` for the area being touched.
3. The relevant project entrypoint:
   - `src/PacketGenerator.Protocol/` for protocol data access.
   - `src/PacketGenerator.Cli/` and `tools/packetgen.cmd` for scriptable read-only access.
   - `src/PacketGenerator.McpStdio/` and `tools/packetgen-mcp.cmd` for stdio MCP.
   - `src/McpServer/` for Web UI, REST API, HTTP MCP, and LLM generation.

## Core Rule

For packet examples, packet history, graph work, edge-case discovery, `switch` analysis,
composition analysis, or type lookup, use the prepared protocol access surfaces first:

```powershell
tools\packetgen.cmd stats --format json
tools\packetgen.cmd packets --filter keep_alive --format json
tools\packetgen.cmd packet play.toClient.keep_alive --format toon
tools\packetgen.cmd composition play.toClient.keep_alive --format json
tools\packetgen.cmd graph --ns play --direction toClient --format json
```

Raw `minecraft-data` protocol files are the input dataset, not the normal agent-facing
inspection surface. Open raw `minecraft-data` files only when investigating a loader/parser
bug, validating an upstream-data discrepancy, or changing the loader itself.

## Public Surfaces

- `PacketGenerator.Protocol` is the protocol access layer. It owns loading,
  history construction, repository queries, schema serialization, composition, graph data,
  stats, and complexity scoring.
- `PacketGenerator.Cli` is the normal stdout CLI for agents and scripts.
- `PacketGenerator.McpStdio` is the stdio MCP server for local MCP clients and MCP Inspector.
- `McpServer` is the Web UI, REST API, HTTP MCP transport, and LLM generation host.

OpenRouter configuration is not required for read-only protocol access. It is required only
for LLM generation paths unless a local model endpoint is configured.

## Change Discipline

- Keep packet/type behavior grounded in `PacketGenerator.Protocol`; avoid duplicating protocol
  parsing in CLI, MCP, REST, or UI layers.
- Do not add a new intermediate representation unless the existing protocol access layer has
  a demonstrated gap.
- Preserve clean stdout for `tools\packetgen.cmd`; diagnostics belong on stderr.
- Preserve MCP framing for `tools\packetgen-mcp.cmd`; do not write unrelated text to stdout.
- If more than three similar packet-generation failures appear, stop bulk generation and
  investigate a systemic mapping, serializer, prompt, or API-sync issue.

## Verification

Use the smallest check that proves the changed surface:

```powershell
dotnet build src/PacketGenerator.Protocol/PacketGenerator.Protocol.csproj -maxcpucount:1
dotnet build src/PacketGenerator.Cli/PacketGenerator.Cli.csproj -maxcpucount:1
dotnet build src/PacketGenerator.McpStdio/PacketGenerator.McpStdio.csproj -maxcpucount:1
dotnet build src/McpServer/McpServer.csproj -maxcpucount:1 -p:BuildClientApp=false
tools\packetgen.cmd stats --format json
```

When changing frontend assets, run the client build intentionally. The server build may also
build frontend assets unless `-p:BuildClientApp=false` is used.
