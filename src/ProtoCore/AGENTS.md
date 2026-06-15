# ProtoCore Agent Instructions

This project loads and validates versioned protocol files before they become the protocol
access layer.

## Responsibilities

- Use `minecraft-data/dataPaths.json` to find protocol versions.
- Load selected `protocol.json` files into `ProtodefProtocol`.
- Validate parsed protocols before repository/history construction.

## Rules

- Keep this layer focused on loading and validation.
- Do not add packet search, stats, graph, or agent-facing output here.
- For packet examples, use `PacketGenerator.Protocol` queries or `tools\packetgen.cmd`.
- Raw file inspection is appropriate here only when the loader or validator is the subject
  of the task.

