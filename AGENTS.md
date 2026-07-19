# minecraft-protocol-fs — Agent Instructions

A standalone F# DSL that describes the Minecraft **Java** protocol as two layers:

```text
model : the external C# shape (ApiField list) that consumers see
wire  : the explicit read/write order, per protocol-version range
```

The point of this repo is to **grow the DSL from simple types to complex ones** by hand, one
type/packet at a time, and eventually **generate C# locally** from the specs.

## The working loop

This is how we work here. Follow it for every new type/packet:

1. **Pick** one protodef type or packet to model (e.g. `entityMetadata`), simplest first.
2. **Fetch the real facts** from PacketGenerator — see next section. Never guess the shape.
3. **Express it** in the DSL in [Program.fs](minecraft-protoccol-fs/Program.fs): `model` (`api [...]`)
   plus one `wire` layout per version range, using `read` / `discard` / `readBlock` / `readUnion` / etc.
4. **Validate**: `dotnet run` prints the whole protocol so you can eyeball the model + wire.
5. **Generate C#** locally into this repo (the codegen target — see "Codegen" below).

Discuss trade-offs (is this a union or a conditional group? inline container or named type?)
before expanding the DSL. Prefer adding one type over broadening the algebra speculatively.

## Where facts come from — NEVER raw minecraft-data

**Hard rule.** Raw `minecraft-data` `protocol.json` files are the *input dataset*, not the
inspection surface. Do **not** read them to model a type — they are namespace-sensitive and easy
to get wrong. Use PacketGenerator's prepared protocol access surfaces instead. (Raw json is only
for debugging PacketGenerator's own loader/parser — not our concern here.)

PacketGenerator lives in a separate checkout. The scripts locate it via `PACKETGEN_ROOT`, falling
back to `..\mcprotonet-workspace\PacketGenerator` then `..\PacketGenerator`. If neither exists,
set `PACKETGEN_ROOT`.

### One-shot lookup → `scripts\facts.cmd`

Wraps PacketGenerator's CLI; args pass straight through.

```powershell
scripts\facts.cmd type entityMetadata --format toon
scripts\facts.cmd packet play.toClient.teams --format toon
scripts\facts.cmd composition play.toClient.map --format json
scripts\facts.cmd packets --filter metadata --format json
scripts\facts.cmd stats --format json
```

Type ids are protodef **camelCase** names (`entityMetadata`, not `entity_metadata`). Packet ids
are `{state}.{direction}.{snake_name}` (`play.toClient.teams`). If unsure of an id, discover it:
`scripts\facts.cmd types --filter metadata` / `scripts\facts.cmd packets --filter team`.

### Many lookups in a session → `scripts\serve-facts.cmd`

Starts PacketGenerator's McpServer **in its own terminal window** (close it to stop). Then hit
REST or HTTP MCP without paying CLI build/startup cost each time:

```text
REST:  GET http://localhost:5000/api/type/{id}
       GET http://localhost:5000/api/schema/{id}
       GET http://localhost:5000/api/composition/{id}
       GET http://localhost:5000/api/packets
MCP:   http://localhost:5000/mcp   (tools: get_type_schema, get_packet_schema, ...)
UI:    http://localhost:5000/
```

Rule of thumb: dereferencing **one** thing → `facts.cmd`; a modelling session touching **many**
types → start the server once with `serve-facts.cmd`.

## Protocol version numbers

The DSL keys `wire` layouts on **protocol numbers**, not Minecraft version strings
(e.g. `Since 764`, `Between(756, 765)`, `Until 763`). Every packet/type schema from the facts
surface already reports the version ranges where each field/shape is present — read them from
there, do not re-derive them from raw json. (Quick sanity anchors: `764 = 1.20.2`, `770 = 1.21.5`.)

## DSL cheat-sheet

All in [Program.fs](minecraft-protoccol-fs/Program.fs) today (types → builders → helpers → specs → printer).

**Builders** (F# computation expressions):

```fsharp
packet "TeamsPacket" Play Clientbound All {           // model + one wire per range
    api  [ field "TeamName" TString All; field "Action" (TUnion "TeamAction") All ]
    wire (Until 764) [ read "team" Str "TeamName"; read "mode" I8 "_mode"; readUnion "_mode" "TeamAction" "Action" ]
    wire (Since 771) [ read "team" Str "TeamName"; read "mode" VarInt "_mode"; readUnion "_mode" "TeamAction" "Action" ]
}

namedType "Rotations" { api [...]; wire All [...] }   // reusable named model + wire
unionType "TeamAction" { cases (Until 764) [ case1 0 "Created" [...]; ... ] }  // discriminated union
```

**Wire entries** (`WireEntry`):

- `read wire type api` — read a wire field into an api field.
- `discard wire type` — read a wire-only field that has no api counterpart (dropped/derived).
- `ifNonZero field entries` — read the block only when a prior field != 0 (conditional group).
- `readOpt wire type api disc keys` — independent optional field selected by a discriminator.
- `readBlock namedType api entries` — inline container promoted to a named model object.
- `readUnion disc unionName api` — dispatch to a top-level named union by discriminator.
- `inlineUnion disc arms` — small local union inline (fallback).
- `SentinelArray(item, endValue)` — read items until a sentinel (e.g. entity metadata, `255`).

**Modelling decisions that carry over from the earlier lab** — apply them here:

- **Not every `switch` is a union.** A switch that only toggles presence → `ifNonZero` /
  conditional group or `Option`. A switch that picks genuinely different payloads → a closed
  `unionType`.
- Wire-only fields with no semantic meaning → `discard`, not an api field.
- Inline containers that deserve a name → `readBlock` into a `namedType`.
- Field-order changes across versions → separate `wire` layouts, same `api`.

## Build / run / verify

```powershell
dotnet run   --project minecraft-protoccol-fs\minecraft-protoccol-fs.fsproj   # prints the protocol
dotnet build minecraft-protocol-fs.slnx                                        # 0 warnings expected
```

SDK is pinned by [global.json](global.json) (net10.0). Format F# with Fantomas before committing.

## Codegen (the target)

Goal: a renderer that emits **C# locally into this repo** from the specs (review artifacts first;
McProtoNet-shaped later). Not built yet — `Program.fs` currently only pretty-prints. When adding
it, write output to a dedicated folder (e.g. `generated-csharp/`) and keep the renderer separate
from the DSL definitions.

## Guardrails

- Never read raw `minecraft-data` json to model a type. Use the facts scripts.
- This repo may become public: don't hardcode personal absolute paths in committed files —
  reach PacketGenerator via `PACKETGEN_ROOT` / the resolver script.
- Grow the DSL deliberately: one type at a time, simple → complex, facts-first.
