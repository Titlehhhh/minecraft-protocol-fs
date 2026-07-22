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

    /// Generate one packet as a source file for the target language.
    let generatePacket (t: ILanguageTarget) (p: PacketSpec) : GeneratedFile =
        {
            RelativePath = sprintf "Packets/%A/%A/%s%s" p.State p.Direction p.ClassName t.Extension
            Contents = t.RenderPacket p
        }

    /// Generate every named type, bitflags and packet in a protocol. Unions are future work.
    let generateProtocol (t: ILanguageTarget) (p: ProtocolSpec) : GeneratedFile list =
        (p.Types |> List.map (generateType t))
        @ (p.Bitflags |> List.map (generateBitflags t))
        @ (p.Packets |> List.map (generatePacket t))
