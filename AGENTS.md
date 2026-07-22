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
   To choose deterministically, ask the facts surface for the dependency-ordered build order
   (`scripts\facts.cmd order --format toon`, or `GET /api/build-order`): types are grouped into
   layers simple→complex, each with its `deps` and a `recursive` flag. Model the lowest-layer
   type whose `deps` are all already done; leave the one `recursive` group (the Slot/SlotComponent
   core) until its layer, since it needs a recursive reference. The UI mirrors this under the Types
   panel's **"By build order"** toggle.
2. **Fetch the real facts** from PacketGenerator — see next section. Never guess the shape.
3. **Express it** in the DSL as **one new file** under [Spec/](minecraft-protocol-fs/Spec). Nothing else
   to wire up: [Spec/Protocol.fs](minecraft-protocol-fs/Spec/Protocol.fs) auto-indexes every spec by
   reflection and the fsproj globs `Spec/**`, so you never edit a shared file (this is what makes
   parallel authoring by subagents conflict-free — one type = one file). Only rule: a spec may
   reference another spec's *binding* by name only if that binding is in `WireAliases.fs` (kept first);
   otherwise reference types by string (`Named "Slot"` / `TNamed "Slot"`), which has no compile-order
   dependency.
   - Plain 1:1 types (wire order = api order, one range) → the **`record`/`col`** sugar; the api is
     *derived* from the wire (`col "x" F32` → `X : TFloat`). Don't hand-write `api`+`wire` for these.
   - Otherwise → `namedType { api [...]; wire (range) [...] }`, `unionType`, or `bitflags` as fits.
4. **Validate**: `dotnet run` prints the whole protocol so you can eyeball the model + wire.
5. **Generate + poke**: `dotnet run -- gen [TypeName]` renders C# into `generated-csharp/`; the
   `sandbox/` projects compile and round-trip it (see "Codegen & sandbox" below).

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
scripts\facts.cmd order --format toon          # types in dependency build order, simple->complex
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
       GET http://localhost:5000/api/build-order   (type dependency layers, simple->complex)
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

## Project layout

The DSL and the concrete specs are separated:

```text
Dsl/     Ast.fs · Builders.fs · Helpers.fs · Printer.fs   — the generic algebra (namespace McProtocol.Dsl)
Codegen/ Target.fs · Generator.fs · CSharpSurface.fs · CSharp.fs — DSL -> source renderer (namespace McProtocol.Codegen)
Spec/    WireAliases.fs · Types|Unions|Bitflags/<Category…>/ · Packets/<State>/<Direction>/<Category…>/ · Protocol.fs  — content (namespace McProtocol.Spec)
Program.fs                                                — entry point: `dotnet run` prints, `-- gen` generates
sandbox/ McProtoNet.Sandbox (lib) · McProtoNet.Sandbox.Console  — compile & poke the generated C#
```

One type / union / bitflags / packet per file; each is an `[<AutoOpen>]` module. `Protocol.fs`
collects every spec by **reflection** (not by name), so files are independent and order-free. DSL
files never reference concrete protocol content; specs `open McProtocol.Dsl`.

Spec folders mirror **MCProtocolLib**'s package layout (GeyserMC/MCProtocolLib —
`packet/ingame/{clientbound,serverbound}/{entity[/player],level[/border],inventory,scoreboard,title,…}`
and `data/game/{entity[/metadata],level[/map],item,chat,…}`). When placing a new spec, put it where
MCProtocolLib puts its counterpart:

```text
Packets/<State>/<Direction>/<Category…>/   Play/Clientbound/Entity/Player/UpdateHealth.fs
                                           Play/Serverbound/Inventory/WindowClick.fs
                                           (misc packets sit at the <Direction> root, like KeepAlive.fs)
Types/<Category…>/                         Math/Vec3f.fs · Entity/Metadata/VillagerData.fs · Level/Map/MapColorData.fs
Unions/<Category…>/                        Entity/Metadata/EntityMetadataValue.fs · Scoreboard/TeamAction.fs
Bitflags/<Category…>/                      Entity/Player/PositionUpdateRelatives.fs
```

New states get sibling folders (`Packets/Login/…`, `Packets/Configuration/…`, `Packets/Status/…`);
`Types/Math/` is ours (MCProtocolLib delegates vectors to an external math lib). Folders are purely
organisational (the fsproj globs `Spec/**`, reflection indexes by value type) — a packet's folder
must simply agree with the spec's own `State`/`Direction` arguments.

## DSL cheat-sheet

Types/builders/helpers/printer live under [Dsl/](minecraft-protocol-fs/Dsl); specs under [Spec/](minecraft-protocol-fs/Spec).

**Builders** (F# computation expressions):

```fsharp
packet "TeamsPacket" Play Clientbound All {           // model + one wire per range
    api  [ field "TeamName" TString All; field "Action" (TUnion "TeamAction") All ]
    wire (Until 764) [ read "team" Str "TeamName"; read "mode" I8 "_mode"; readUnion "_mode" "TeamAction" "Action" ]
    wire (Since 771) [ read "team" Str "TeamName"; read "mode" VarInt "_mode"; readUnion "_mode" "TeamAction" "Action" ]
}

namedType "Rotations" { api [...]; wire All [...] }   // reusable named model + wire
unionType "TeamAction" { cases (Until 764) [ case1 0 "Created" [...]; ... ] }  // discriminated union

// plain 1:1 type — api derived from wire, no `api` block (see Dsl/Helpers.fs: apiOf/col/record):
record "Vec4f" (Since 762) [ col "x" F32; col "y" F32; col "z" F32; col "w" F32 ]

// bitflags — named bits become bool api fields (union of all layouts); multi-version via >1 layout:
bitflags "PositionUpdateRelatives" {
    layout (Between(766, 767)) U8  [ "x"; "y"; "z"; "yaw"; "pitch" ]
    layout (Since 768)         U32 [ "x"; "y"; "z"; "yaw"; "pitch"; "dx"; "dy"; "dz"; "yawDelta" ]
}
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
dotnet run   --project minecraft-protocol-fs\minecraft-protocol-fs.fsproj            # prints the protocol
dotnet run   --project minecraft-protocol-fs\minecraft-protocol-fs.fsproj -- gen     # generate C#
dotnet run   --project sandbox\McProtoNet.Sandbox.Console                            # round-trip the generated types
dotnet build minecraft-protocol-fs.slnx                                              # whole solution, 0 warnings
```

SDK is pinned by [global.json](global.json) (net10.0). Format F# with Fantomas before committing.

## Codegen & sandbox

Codegen lives in [Codegen/](minecraft-protocol-fs/Codegen), separate from the DSL. The DSL AST is the
language-neutral IR; a backend implements `ILanguageTarget` ([Target.fs](minecraft-protocol-fs/Codegen/Target.fs))
and `Generator` drives it — one file per type. Adding a language = one new target, nothing else
changes. Run: `dotnet run -- gen` (whole protocol) or `dotnet run -- gen Vec4f` (one type, echoed to
stdout). Output goes to `generated-csharp/`, mirroring the spec layout: `Types/`, `Bitflags/`,
`Packets/<State>/<Direction>/`. Packets render exactly like named types (class/record struct +
Read/Write); a packet whose wire uses still-unsupported entries comes out as a compiling stub with
`// TODO(codegen)` markers and `default!` ctor args.

The C# backend is built on **Roslyn**: type/method structure via `SyntaxFactory`, statement bodies
via `ParseStatement`, `NormalizeWhitespace` for formatting, and `GetDiagnostics` as a hard gate — a
file that does not parse as valid C# fails generation instead of being written. Every runtime name
the output references (reader/writer types, `ThrowHelper`, `ProtocolSupport`, the per-wire-type
method map) lives in one record, [CSharpSurface.fs](minecraft-protocol-fs/Codegen/CSharpSurface.fs)'s
`RuntimeSurface`; `CSharp.targetFor someSurface` retargets the same renderer at a renamed or
different runtime, `CSharp.target` is the McProtoNet default.

**Target shape = McProtoNet** ([CSharp.fs](minecraft-protocol-fs/Codegen/CSharp.fs)): value types →
`[ProtocolSupport(from,to)] public readonly partial record struct Name(...)`, reference types →
`sealed partial class`; namespace `McProtoNet.Protocol`. Read/Write live **together** in the type body
(static `Read(ref MinecraftPrimitiveReader, int)` + instance `Write(MinecraftPrimitiveWriter, int)`)
against the McProtoNet primitive surface, gated by `ThrowHelper.ThrowIfProtocolNotSupported<T>`.

**Runtime primitives vs generated.** Some leaf types are provided by the runtime, not generated:
`position` (a bitfield packed in a long — decompose in the DSL is not worth it for the only bitfield;
McProtoNet hand-writes it), plus `string` / `ByteArray` / `optvarint` / `Uuid` / `NBT`. Reference them
as `Named "Position"` etc. (→ `ReadType<Position>`), do **not** write a spec for them. In contrast,
**bitflags** *are* generated (record-struct-of-bools + bit pack); they are a family with per-type
names, so they earned the `bitflags` construct.

**Sandbox** ([sandbox/](sandbox)) makes the generated C# real: `McProtoNet.Sandbox` is a minimal
McProtoNet-shaped runtime ([Runtime.cs](sandbox/McProtoNet.Sandbox/Runtime.cs): the attribute,
`MinecraftVersion`, `ThrowHelper`, big-endian reader/writer) plus the generated types linked from
`generated-csharp/` (incomplete ones excluded in the csproj). `McProtoNet.Sandbox.Console` round-trips
them. **Compiling the sandbox is the codegen's real test** — it caught a scope bug the printer could not.

**Multi-version works**: every wire layout becomes a `protocolVersion`-guarded branch in Read/Write
(single-layout types stay flat; a version inside the support span but outside every layout throws
`NotSupportedException`). `Array`/`Option`/`ByteArray`/`RestBytes`/`discard` wire entries are
implemented; nested named types dispatch through `IProtocolType<TSelf>` (static abstract interface —
zero reflection, AOT-safe; benchmarked ≈ direct call).

**Known codegen gaps** (emit a visible `// TODO(codegen)`, never wrong silent code): unions
(`readUnion`/`inlineUnion`), `readBlock`, `readOpt`, `ifNonZero`, `SentinelArray`, and named-type
morph across versions (Explosion's `option vec3f` @768 → `option vec3f64` @769). Stubs still
compile; specs whose *api* references a not-yet-modelled C# type (Particle, Slot, union types, NBT
in the sandbox) are excluded in the sandbox csproj until those land.

## Guardrails

- Never read raw `minecraft-data` json to model a type. Use the facts scripts.
- This repo may become public: don't hardcode personal absolute paths in committed files —
  reach PacketGenerator via `PACKETGEN_ROOT` / the resolver script.
- Grow the DSL deliberately: one type at a time, simple → complex, facts-first.
- Keep spec files (`Spec/**`) clean — **no `//` comments**; the `record`/`namedType`/`bitflags` form
  should read for itself. (Comments in `Dsl/`/`Codegen/` algebra are fine.)
