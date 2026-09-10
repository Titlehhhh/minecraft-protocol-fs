namespace McProtocol.Codegen

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

/// C# backend on Roslyn, shaped like McProtoNet's `McProtoNet.Protocol.Types`.
///
/// Division of labour:
///   - *structure* (usings, namespace, attributes, type shells, method signatures) is built with
///     `SyntaxFactory`, so the shape of a generated file is data, not string concatenation;
///   - *statements* are parsed from short text lines (`ParseStatement`), which keeps the read/write
///     bodies as readable as the strings they produce; `// TODO(codegen)` markers ride along as
///     trivia;
///   - `NormalizeWhitespace` formats, and `GetDiagnostics` is the safety net: a file that does not
///     parse as valid C# fails generation loudly instead of being written out.
///
/// Multi-version: every wire layout becomes a protocol-version-guarded branch in Read/Write (the
/// same shape bitflags always had); a version inside the support span but outside every layout
/// throws. Every runtime name the output references comes from a `CSharpSurface.RuntimeSurface` —
/// see `targetFor` to retarget the same renderer at a different runtime.
module CSharp =

    open type SyntaxFactory
    open McProtocol.Codegen.CSharpBackend.NamedTypes
    open McProtocol.Codegen.CSharpBackend.PacketLayers
    open McProtocol.Codegen.CSharpBackend.Bitflags
    open McProtocol.Codegen.CSharpBackend.Enums
    open McProtocol.Codegen.CSharpBackend.Unions
    open McProtocol.Codegen.CSharpBackend.Aggregates

    // ----- target -----

    /// A C# code-generation target bound to a specific runtime surface.
    let targetFor (surface: RuntimeSurface) : ILanguageTarget =
        { new ILanguageTarget with
            member _.Id = "csharp"
            member _.Extension = ".cs"

            member _.RenderType spec =
                renderType surface surface.Namespace surface.ProtocolInterface spec [] []

            member _.RenderBitflags spec = renderBitflags surface spec
            member _.RenderEnum spec = renderEnum surface spec
            member _.RenderUnion spec = renderUnion surface spec
            member _.RenderPacket entry = renderPacket surface entry
            member _.RenderProtocol entries = renderProtocolExtras surface entries
        }

    /// The default C# target: the McProtoNet runtime surface.
    let target: ILanguageTarget = targetFor mcProtoNet
