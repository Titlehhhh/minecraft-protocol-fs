namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Bitflags =

    open type SyntaxFactory
    open Text
    open Structure
    open Bodies
    open Json

    // ----- bitflags -----

    let internal renderBitflags (s: RuntimeSurface) (spec: BitflagsSpec) : string =
        let name = spec.Name
        let apiFlags = spec.Layouts |> List.collect (fun l -> l.Flags) |> List.distinct

        let readCore (l: BitflagsLayout) =
            match integralBacking s l.Backing with
            | None -> [ todoLine (sprintf "backing %A" l.Backing); throwTodoLine name ]
            | Some p ->
                let args =
                    apiFlags
                    |> List.map (fun f ->
                        match List.tryFindIndex ((=) f) l.Flags with
                        | Some i -> sprintf "(flags & (1 << %d)) != 0" i
                        | None -> "false")
                    |> String.concat ", "

                [
                    sprintf "%s flags = %s.%s;" p.CsType s.ReaderParam p.ReadCall
                    sprintf "return new %s(%s);" name args
                ]

        let writeCore (l: BitflagsLayout) =
            match integralBacking s l.Backing with
            | None -> [ todoLine (sprintf "backing %A" l.Backing); throwTodoLine name ]
            | Some p ->
                [
                    yield sprintf "%s flags = 0;" p.CsType
                    for i, f in List.indexed l.Flags -> sprintf "if (%s) flags |= (1 << %d);" (pascal f) i
                    yield sprintf "%s.%s(flags);" s.WriterParam p.WriteMethod
                ]

        let readBody =
            gateLine s name
            :: versionedBody s name false [ for l in spec.Layouts -> l.Range, readCore l ]

        let writeBody =
            gateLine s name
            :: versionedBody s name true [ for l in spec.Layouts -> l.Range, writeCore l ]

        let jsonBody =
            objectLines s [ for f in apiFlags -> pascal f, JBool, pascal f ]

        let shell =
            (recordStructShell s.ProtocolInterface name (apiFlags |> List.map (fun f -> "bool", pascal f)))
                .AddMembers(
                    readMethod s name (parseBody readBody),
                    writeMethod s true (parseBody writeBody),
                    writeJsonMethod s true (parseBody jsonBody)
                )
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        renderUnit s s.Namespace name [ s.UsingAttributes; s.UsingSerialization; s.UsingJson ] shell
