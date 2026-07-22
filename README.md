# minecraft-protocol-fs

A hand-written F# DSL for the Minecraft **Java** protocol. Each packet/type is described as a
`model` (the external C# shape) plus one or more `wire` layouts (explicit read/write order per
protocol-version range). The DSL is grown by hand from simple types to complex ones, with C#
generated locally from the specs.

## Layout

```text
minecraft-protocol-fs/Program.fs   the DSL (types, builders, helpers) + sample specs + a printer
scripts/facts.cmd                   one-shot protocol fact lookup (PacketGenerator CLI)
scripts/serve-facts.cmd             start the facts server in its own window (REST + HTTP MCP)
AGENTS.md                           how to work in this repo (read this)
```

## Quick start

```powershell
dotnet run --project minecraft-protocol-fs\minecraft-protocol-fs.fsproj
```

## Getting protocol facts

Model shapes come from **PacketGenerator's prepared surfaces**, never from raw `minecraft-data`
json. Point `PACKETGEN_ROOT` at a PacketGenerator checkout (or keep it beside
`mcprotonet-workspace`), then:

```powershell
scripts\facts.cmd type entity_metadata --format toon     # one lookup
scripts\serve-facts.cmd                                   # many lookups (REST/MCP on :5000)
```

See [AGENTS.md](AGENTS.md) for the full workflow and DSL reference.

## Editor

F# is developed in VS Code with the **Ionide** extension (recommended set in
[.vscode/extensions.json](.vscode/extensions.json)); Rider is kept for heavy debugging.
