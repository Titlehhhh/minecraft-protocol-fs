# PacketGenerator.Protocol Agent Instructions

This project is the shared protocol access layer. Keep packet/type loading, history building,
query behavior, schema serialization, graph data, stats, and complexity scoring here.

## Responsibilities

- Load the configured protocol range through `ProtocolDataLoader`.
- Build versioned histories through `HistoryBuilder`.
- Separate packet mappers from named types through `ProtocolRepository`.
- Expose read-only queries through `ProtocolQueryService`.
- Serialize schemas through `ProtocolSchemaSerializer`.
- Build graph data through `ProtocolGraphBuilder`.

## Rules

- Do not duplicate protocol parsing in CLI, stdio MCP, REST endpoints, or frontend code.
- Do not inspect raw `minecraft-data` as a substitute for repository/query APIs.
- Keep public APIs deterministic and side-effect free where possible.
- Preserve both JSON and TOON output paths.
- When changing packet/type structure handling, test at least one packet, one named type,
  stats, and composition through `tools\packetgen.cmd`.

## Packet Mapper Notes

Packets are derived from packet mapper histories, not from arbitrary type names.
`ProtocolRepository` expects packet mapper containers to contain `name` and `params`, where
`name` maps ids to packet names and `params` switches from packet names to payload types.

Namespace-aware lookup matters. Shared/global types can be valid packet payloads.

