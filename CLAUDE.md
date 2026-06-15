# Claude Code Instructions - PacketGenerator

PacketGenerator is a public repository for Minecraft protocol packet discovery and code
generation. It exposes a reusable protocol access layer through CLI, stdio MCP, REST,
HTTP MCP, Web UI, and LLM generation surfaces.

Read `AGENTS.md` and `AI_CONTEXT.md` before doing protocol work.

## Preferred Agent Entry Points

For read-only protocol inspection, prefer the clean stdout CLI:

```powershell
tools\packetgen.cmd stats --format json
tools\packetgen.cmd packets --filter keep_alive --format json
tools\packetgen.cmd packet play.toClient.keep_alive --format toon
tools\packetgen.cmd type ArmorTrimMaterial --format json
tools\packetgen.cmd composition play.toClient.keep_alive --format json
```

For stdio MCP:

```powershell
tools\packetgen-mcp.cmd
```

For Web UI, REST, HTTP MCP, and generation, run `src/McpServer`.

## Important Boundary

Do not use raw `minecraft-data` protocol JSON as the normal way to inspect packet examples.
Use `PacketGenerator.Protocol`, the CLI, stdio MCP, or REST endpoints. Raw protocol files are
for loader/parser debugging and upstream-data discrepancy checks.

OpenRouter is not required for read-only access. It is required only for LLM generation paths
unless a local endpoint is configured.

