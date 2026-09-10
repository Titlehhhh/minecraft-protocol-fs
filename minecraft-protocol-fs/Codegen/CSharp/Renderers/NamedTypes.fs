namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module NamedTypes =

    open type SyntaxFactory
    open Text
    open Structure
    open Bodies

    // ----- named types -----

    let internal usingsFor (s: RuntimeSurface) (fields: ApiField list) =
        let text = fields |> List.map (fun f -> csType s f.Type) |> String.concat " "

        [
            yield s.UsingAttributes
            yield s.UsingSerialization
            if text.Contains s.NbtType then
                yield s.UsingNbt
            if text.Contains s.UuidType then
                yield s.UsingSystem
        ]

    let internal renderType
        (s: RuntimeSurface)
        (ns: string)
        (iface: string option)
        (spec: NamedTypeSpec)
        (extraAttrs: AttributeListSyntax list)
        (extraMembers: MemberDeclarationSyntax list)
        : string
        =
        let value = spec.ApiFields |> List.forall (fun f -> isValue f.Type)
        let fields = spec.ApiFields |> List.map (fun f -> csType s f.Type, f.Name)
        let apiTypes = spec.ApiFields |> List.map (fun f -> f.Name, f.Type) |> Map.ofList

        let readBody =
            gateLine s spec.Name
            :: versionedBody
                s
                spec.Name
                false
                [
                    for l in spec.Layouts -> l.Range, layoutReadLines s spec.Name spec.ApiFields l
                ]

        let writeBody =
            gateLine s spec.Name
            :: versionedBody
                s
                spec.Name
                true
                [ for l in spec.Layouts -> l.Range, layoutWriteLines s spec.Name apiTypes l ]

        let shell =
            if value then
                recordStructShell iface spec.Name fields
            else
                classShell iface spec.Name fields

        let shell =
            shell
                .AddMembers(readMethod s spec.Name (parseBody readBody), writeMethod s value (parseBody writeBody))
                .AddMembers(List.toArray extraMembers)
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))
                .AddAttributeLists(List.toArray extraAttrs)

        renderUnit s ns spec.Name (usingsFor s spec.ApiFields) shell
