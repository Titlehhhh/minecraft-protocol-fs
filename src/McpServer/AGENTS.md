# McpServer Agent Instructions

`McpServer` hosts the Web UI, REST API, and HTTP MCP transport.
It consumes `PacketGenerator.Protocol`; it should not own protocol parsing or history logic.

## Rules

- Keep REST/MCP data reads backed by `IProtocolRepository` and `ProtocolQueryService`.
- Do not duplicate CLI behavior here unless the surface is genuinely HTTP-specific.
- Use `-p:BuildClientApp=false` for backend-only builds.

## Useful Checks

```powershell
dotnet build src/McpServer/McpServer.csproj -maxcpucount:1 -p:BuildClientApp=false
dotnet run --project src/McpServer/McpServer.csproj --no-build -- --port 5000
```

