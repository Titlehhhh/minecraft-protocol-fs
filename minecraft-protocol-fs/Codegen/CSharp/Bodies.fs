namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Bodies =

    open type SyntaxFactory
    open Text
    open Statements

    // ----- per-layout bodies + version branching -----

    let internal layoutReadLines
        (s: RuntimeSurface)
        (name: string)
        (apiFields: ApiField list)
        (l: WireLayout)
        : string list
        =
        let results = readEntriesLines s l.Entries

        let errors =
            results
            |> List.choose (function
                | _, Error e -> Some e
                | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            let bound = boundLocals s results
            let lines = readLinesOf results

            let ctorArgs =
                apiFields
                |> List.map (fun f ->
                    if bound.Contains(localName s f.Name) then
                        localName s f.Name
                    else
                        "default!")
                |> String.concat ", "

            lines @ [ sprintf "return new %s(%s);" name ctorArgs ]

    let internal layoutWriteLines
        (s: RuntimeSurface)
        (name: string)
        (apiTypes: Map<string, ApiType>)
        (l: WireLayout)
        : string list
        =
        let results =
            l.Entries |> List.map (writeEntryLines s (writeCtxOf apiTypes l.Entries))

        let errors =
            results
            |> List.choose (function
                | Error e -> Some e
                | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            results
            |> List.collect (function
                | Ok ls -> ls
                | Error _ -> [])

    /// One guarded branch per layout. A single unconditional layout stays flat (the support
    /// attribute already gates its span); with several layouts, a version that matches none throws.
    let internal versionedBody
        (s: RuntimeSurface)
        (typeName: string)
        (isWrite: bool)
        (layouts: (VersionRange * string list) list)
        : string list
        =
        match layouts with
        | [] -> [ throwNoLayoutLine s typeName ]
        | [ (_, body) ] -> body
        | _ ->
            let hasCatchAll = layouts |> List.exists (fun (r, _) -> guardCondition s r = None)

            [
                for r, body in layouts do
                    let endsWithThrow =
                        body |> List.tryLast |> Option.exists (fun (l: string) -> l.StartsWith "throw")

                    let body =
                        if isWrite && not endsWithThrow then
                            body @ [ "return;" ]
                        else
                            body

                    match guardCondition s r with
                    | Some c -> yield! [ sprintf "if (%s)" c; "{" ] @ body @ [ "}" ]
                    | None -> yield! [ "{" ] @ body @ [ "}" ]
            ]
            @ (if hasCatchAll then [] else [ throwNoLayoutLine s typeName ])
