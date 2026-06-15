# PacketGenerator.McpStdio Agent Instructions

This project is the stdio MCP surface for local MCP clients and MCP Inspector.

## Rules

- Preserve stdio MCP framing. Do not write unrelated text to stdout.
- Keep tools thin over `PacketGenerator.Protocol`.
- Add read-only discovery tools here when agents need packet/type/graph context without
  starting the HTTP server.
- Prefer `tools\packetgen-mcp.cmd` for local use because it builds first.

