# minecraft-protocol-fs

A hand-written F# DSL for the Minecraft **Java** protocol. Each packet/type is described as a
`model` (the external C# shape) plus one or more `wire` layouts (explicit read/write order per
protocol-version range). The DSL is grown by hand from simple types to complex ones, with C#
generated locally from the specs.

## Layout

```text
minecraft-protocol-fs/              the F# project
  Dsl/                              the generic algebra (types, builders, helpers, printer)
  Codegen/                          DSL -> C# renderer (Roslyn-backed)
  Spec/                             protocol content — one type/packet per file
  Program.fs                        entry point: `dotnet run` prints, `-- gen` generates
generated-csharp/                   codegen output (C# mirrored from Spec/)
sandbox/                            minimal McProtoNet-shaped runtime that compiles & round-trips the generated C#
scripts/facts.cmd                   one-shot protocol fact lookup (McProtoFacts CLI)
scripts/serve-facts.cmd             start the facts server in its own window (REST + HTTP MCP)
AGENTS.md                           how to work in this repo (read this)
```

## Quick start

```powershell
dotnet run --project minecraft-protocol-fs\minecraft-protocol-fs.fsproj
```

## Getting protocol facts

Model shapes come from **McProtoFacts' prepared surfaces**, never from raw `minecraft-data`
json. Point `MCPROTO_FACTS_ROOT` at a mcproto-facts checkout (or keep it beside
`mcprotonet-workspace`), then:

```powershell
scripts\facts.cmd type entityMetadata --format toon      # one lookup
scripts\serve-facts.cmd                                   # many lookups (REST/MCP on :5000)
```

See [AGENTS.md](AGENTS.md) for the full workflow and DSL reference.

## Editor

F# is developed in VS Code with the **Ionide** extension (recommended set in
[.vscode/extensions.json](.vscode/extensions.json)); Rider is kept for heavy debugging.
