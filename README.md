# minecraft-protocol-fs

[![CI](https://github.com/Titlehhhh/minecraft-protocol-fs/actions/workflows/ci.yml/badge.svg)](https://github.com/Titlehhhh/minecraft-protocol-fs/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A hand-written F# DSL that describes the Minecraft **Java Edition** protocol and
generates the C# protocol layer for [McProtoNet](https://github.com/Titlehhhh/McProtoNet),
a C# Minecraft client library.

One spec file describes one packet: an `api` block gives the external C# shape,
and one or more `wire` layouts give the exact read order for a protocol-version
range. This is [`Spec/Packets/Login/Clientbound/LoginSuccess.fs`](minecraft-protocol-fs/Spec/Packets/Login/Clientbound/LoginSuccess.fs), trimmed:

```fsharp
packet "LoginSuccessPacket" Login Clientbound All {
    protoId "success"

    api [
        field "Uuid"       TUuid                              All
        field "Username"   TString                            All
        field "Properties" (TArray(TNamed "ProfileProperty")) (Since 759)
    ]

    wire (Until 758) [
        read "uuid"     Uuid "Uuid"
        read "username" Str  "Username"
    ]

    wire (Between(759, 765)) [
        read "uuid"       Uuid "Uuid"
        read "username"   Str  "Username"
        read "properties" (Array(Named "ProfileProperty", VarIntCount)) "Properties"
    ]

    // ...two more layouts for 766–767 and 768+
}
```

The codegen turns it into one C# class. Every wire layout becomes a
version-guarded branch:

```csharp
public sealed partial class LoginSuccessPacket : IProtocolType<LoginSuccessPacket>
{
    public Guid Uuid { get; }
    public string Username { get; }
    public ProfileProperty[] Properties { get; }

    public static LoginSuccessPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        if (protocolVersion <= 758)
        {
            var uuid = reader.ReadUUID();
            var username = reader.ReadString();
            return new LoginSuccessPacket(uuid, username, default!, default!);
        }
        // ...one branch per wire layout, plus Write and GetPacketId
    }
}
```

Version ranges use protocol numbers, not version strings (764 = 1.20.2,
770 = 1.21.5). The target range is protocols 735–772, that is Minecraft 1.16
through 1.21.8.

## Status

Work in progress. The DSL grows by hand, from simple packets to complex ones.
52 packet specs exist so far:

| State         | Clientbound | Serverbound |
|---------------|-------------|-------------|
| Handshaking   | —           | 2           |
| Status        | 2           | 2           |
| Login         | 6           | 5           |
| Configuration | 5           | 5           |
| Play          | 19          | 6           |

Shared shapes: 16 types, 2 bitflag sets, 2 unions. The real protocol has far
more Play packets, so most of that state is still ahead. A few complex specs
(EntityMetadata, Teams, Explosion) already exist in the DSL but stay out of the
sandbox build until the types they reference land.

## Repository layout

| Path | Purpose |
|------|---------|
| [`minecraft-protocol-fs/`](minecraft-protocol-fs/) | The F# project: `Dsl/` (the algebra: types, builders, printer), `Codegen/` (DSL → C#, Roslyn-backed), `Spec/` (one packet or type per file) |
| `generated-csharp/` | Codegen output. Not tracked in git — `dotnet run -- gen` rebuilds it |
| [`sandbox/`](sandbox/) | A minimal McProtoNet-shaped runtime. It compiles the generated C# and round-trips it |
| [`facts/`](facts/) | McProtoFacts, the protocol facts provider (CLI, REST, MCP server). See below |
| [`scripts/`](scripts/) | Wrappers for fact lookups and the facts server |
| [`AGENTS.md`](AGENTS.md) | The full working guide and DSL reference |

## Build

```
git clone --recursive https://github.com/Titlehhhh/minecraft-protocol-fs
```

The `--recursive` flag matters: `facts/minecraft-data` is a git submodule.

Two SDKs live in this repo. The root builds with .NET 10 (root
[`global.json`](global.json)). `facts/` builds with its own .NET 11 preview SDK
(`facts/global.json`) and does not affect the root build.

The pipeline, from the repo root:

```powershell
# 1. print the protocol model
dotnet run --project minecraft-protocol-fs/minecraft-protocol-fs.fsproj

# 2. generate C# into generated-csharp/
dotnet run --project minecraft-protocol-fs/minecraft-protocol-fs.fsproj -- gen

# 3. compile the generated code in the sandbox
dotnet build minecraft-protocol-fs.slnx
```

CI runs the same steps on every push. F# work happens in VS Code with the
**Ionide** extension (recommended set in
[.vscode/extensions.json](.vscode/extensions.json)). Rider works too.

## Where the facts come from: McProtoFacts

Every wire layout needs a source of truth. The upstream dataset is
[`PrismarineJS/minecraft-data`](https://github.com/PrismarineJS/minecraft-data),
but its raw json is easy to misread. So this repo never reads it directly.
[`facts/`](facts/) holds **McProtoFacts**, a tool that builds a versioned model
from `minecraft-data` and serves prepared, readable surfaces:

```powershell
scripts\facts.cmd type entityMetadata --format toon   # one lookup
scripts\serve-facts.cmd                               # REST + MCP server on :5000
```

Spec writing is a constant loop of fact lookups, so the facts tool lives next
to the specs. The wrapper scripts start it from its own folder with its own
SDK — no extra setup.

## License

[MIT](LICENSE).
