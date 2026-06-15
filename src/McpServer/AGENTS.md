# McpServer Agent Instructions

`McpServer` hosts the Web UI, REST API, HTTP MCP transport, and LLM generation workflows.
It consumes `PacketGenerator.Protocol`; it should not own protocol parsing or history logic.

## Rules

- Read-only REST and HTTP MCP access must work without an OpenRouter key.
- OpenRouter is required only for LLM generation paths unless a local endpoint is configured.
- Keep REST/MCP data reads backed by `IProtocolRepository` and `ProtocolQueryService`.
- Do not duplicate CLI behavior here unless the surface is genuinely HTTP-specific.
- Use `-p:BuildClientApp=false` for backend-only builds.

## Useful Checks

```powershell
dotnet build src/McpServer/McpServer.csproj -maxcpucount:1 -p:BuildClientApp=false
dotnet run --project src/McpServer/McpServer.csproj --no-build -- --port 5000
```

