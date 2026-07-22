namespace McProtocol.Codegen

open McProtocol.Dsl

/// Drives an `ILanguageTarget` over the DSL AST: one source file per named type. Language-agnostic.
module Generator =

    /// Generate one named type as a source file for the target language.
    let generateType (t: ILanguageTarget) (spec: NamedTypeSpec) : GeneratedFile =
        { RelativePath = spec.Name + t.Extension
          Contents     = t.RenderType spec }

    /// Generate one bitflags type as a source file for the target language.
    let generateBitflags (t: ILanguageTarget) (spec: BitflagsSpec) : GeneratedFile =
        { RelativePath = spec.Name + t.Extension
          Contents     = t.RenderBitflags spec }

    /// Generate every named type and bitflags in a protocol. Unions and packets are future work.
    let generateProtocol (t: ILanguageTarget) (p: ProtocolSpec) : GeneratedFile list =
        (p.Types |> List.map (generateType t))
        @ (p.Bitflags |> List.map (generateBitflags t))
