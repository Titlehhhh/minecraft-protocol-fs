namespace McProtocol.Codegen

open McProtocol.Dsl

/// Drives an `ILanguageTarget` over the DSL AST: one source file per spec. Language-agnostic.
/// Output mirrors the Spec folder shape: `Types/`, `Bitflags/`, `Packets/<State>/<Direction>/`.
module Generator =

    /// Generate one named type as a source file for the target language.
    let generateType (t: ILanguageTarget) (spec: NamedTypeSpec) : GeneratedFile =
        {
            RelativePath = sprintf "Types/%s%s" spec.Name t.Extension
            Contents = t.RenderType spec
        }

    /// Generate one bitflags type as a source file for the target language.
    let generateBitflags (t: ILanguageTarget) (spec: BitflagsSpec) : GeneratedFile =
        {
            RelativePath = sprintf "Bitflags/%s%s" spec.Name t.Extension
            Contents = t.RenderBitflags spec
        }

    /// Generate one union type as a source file for the target language.
    let generateUnion (t: ILanguageTarget) (spec: UnionTypeSpec) : GeneratedFile =
        {
            RelativePath = sprintf "Unions/%s%s" spec.Name t.Extension
            Contents = t.RenderUnion spec
        }

    /// Generate one packet as a source file for the target language.
    let generatePacket (t: ILanguageTarget) (e: Registry.CatalogEntry) : GeneratedFile =
        {
            RelativePath = sprintf "Packets/%A/%A/%s%s" e.Spec.State e.Spec.Direction e.Spec.ClassName t.Extension
            Contents = t.RenderPacket e
        }

    /// Generate every named type, bitflags, union and packet in a protocol, plus the target's
    /// whole-protocol aggregates. Packets go through the ordinal catalog: identity and ordinals
    /// come from there.
    let generateProtocol (t: ILanguageTarget) (p: ProtocolSpec) : GeneratedFile list =
        let catalog = Registry.catalog p.Packets

        (p.Types |> List.map (generateType t))
        @ (p.Bitflags |> List.map (generateBitflags t))
        @ (p.Unions |> List.map (generateUnion t))
        @ (catalog |> List.map (generatePacket t))
        @ t.RenderProtocol p
