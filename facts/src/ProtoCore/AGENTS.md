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
- Raw `minecraft-data`: the rule and its only exceptions live in the facts root `AGENTS.md`.

