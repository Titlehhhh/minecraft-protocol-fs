namespace McProtocol.Codegen

open McProtocol.Dsl

/// Code generation: turn a DSL ProtocolSpec into source files for a target language.
///
/// The DSL AST (`NamedTypeSpec` / `ProtocolSpec`) is the language-neutral IR. A target renders it
/// to source. Adding a language means implementing one `ILanguageTarget` — the generator that
/// drives it stays the same. (Kept deliberately thin: the first real backend showed a shared
/// statement-IR fights the target's idioms, so each target owns its own rendering.)
/// A rendered source file; path is relative to the chosen output root.
type GeneratedFile =
    {
        RelativePath: string
        Contents: string
    }

/// A codegen backend for one target language.
type ILanguageTarget =
    /// Short id, e.g. "csharp".
    abstract Id: string
    /// File extension including the dot, e.g. ".cs".
    abstract Extension: string
    /// Render one named type into a complete source file body.
    abstract RenderType: NamedTypeSpec -> string
    /// Render one bitflags type into a complete source file body.
    abstract RenderBitflags: BitflagsSpec -> string
    /// Render one enum type into a complete source file body.
    abstract RenderEnum: EnumSpec -> string
    /// Render one union type into a complete source file body.
    abstract RenderUnion: UnionTypeSpec -> string
    /// Render one packet (with its ordinal-catalog entry) into a complete source file body.
    abstract RenderPacket: Registry.CatalogEntry -> string
    /// Render whole-protocol aggregate outputs (registry tables, dispatcher, handler bases).
    /// Takes the full spec: aggregates must know which named types exist to keep references
    /// resolvable. A target with no aggregates returns [].
    abstract RenderProtocol: ProtocolSpec -> GeneratedFile list

/// Shared version-range helpers targets can lean on.
module VersionRangeX =

    /// Inclusive numeric bounds of a range: `(lower, upper)`, `None` meaning unbounded.
    let bounds =
        function
        | All -> None, None
        | Since v -> Some v, None
        | Until v -> None, Some v
        | Between(a, b) -> Some a, Some b

    /// The overall span a set of ranges covers. A `None` end anywhere makes that end unbounded.
    let span (ranges: VersionRange list) : int option * int option =
        let ends pick =
            let parts = ranges |> List.map (bounds >> pick)

            if List.contains None parts then
                None
            else
                match List.choose id parts with
                | [] -> None
                | xs -> Some xs

        ends fst |> Option.map List.min, ends snd |> Option.map List.max
