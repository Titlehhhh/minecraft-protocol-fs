namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Enums =

    open type SyntaxFactory
    open Text
    open Structure
    open Bodies
    open Json

    // ----- enums -----

    /// `push_other_teams` -> `PushOtherTeams`. The wire names are protodef mapper labels, so
    /// snake_case is the only shape they come in; a leading digit and the record's own `Value`
    /// parameter are the two spellings C# would reject.
    let private constName (typeName: string) (wireName: string) =
        let parts =
            wireName.Split([| '_'; '.'; '/'; ':' |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map pascal

        let joined = String.concat "" parts

        let joined =
            if joined = "" || System.Char.IsDigit joined.[0] then
                "_" + joined
            else
                joined

        if joined = "Value" || joined = typeName then
            joined + "_"
        else
            joined

    /// Parse a whole member declaration (field, operator, expression-bodied method) from text.
    let private parseMember (text: string) : MemberDeclarationSyntax =
        match ParseMemberDeclaration text with
        | null -> failwithf "codegen: member did not parse:\n%s" text
        | m -> m

    /// `Value switch { 0 => "content", ..., _ => $"unknown({Value})" }` over one table.
    let private enumSwitchExpr (values: (int * string) list) =
        let arms =
            [
                for id, name in values -> sprintf "%d => \"%s\"," id name
                yield "_ => $\"unknown({Value})\""
            ]

        String.concat "\n" ([ "Value switch"; "{" ] @ arms @ [ "}" ])

    let internal renderEnum (s: RuntimeSurface) (spec: EnumSpec) : string =
        let name = spec.Name

        // Constants are the union of every layout's table: a value the current version does not
        // know is still a value the type can hold, and callers compare against one set of names.
        let merged =
            spec.Layouts
            |> List.collect (fun l -> l.Values)
            |> List.fold
                (fun acc (id, v) ->
                    if acc |> List.exists (fun (_, other) -> other = v) then
                        acc
                    else
                        acc @ [ id, v ])
                []

        let conflicts =
            merged
            |> List.filter (fun (id, v) ->
                spec.Layouts
                |> List.exists (fun l -> l.Values |> List.exists (fun (i2, v2) -> v2 = v && i2 <> id)))

        // A switch cannot carry the same case label twice: when a later layout renames an id, the
        // merged `ToString()` answers with the first name and the versioned overload tells them
        // apart. The names still all become constants — only the switch has to dedupe by id.
        let mergedByFirstId =
            merged
            |> List.fold
                (fun acc (id, v) ->
                    if acc |> List.exists (fun (other, _) -> other = id) then
                        acc
                    else
                        acc @ [ id, v ])
                []

        let constants: MemberDeclarationSyntax list =
            [
                for id, v in merged ->
                    parseMember (sprintf "public static readonly %s %s = new(%d);" name (constName name v) id)
            ]

        let conversions: MemberDeclarationSyntax list =
            [
                parseMember (sprintf "public static explicit operator int(%s value) => value.Value;" name)
                parseMember (sprintf "public static explicit operator %s(int value) => new(value);" name)
            ]

        let toStringMembers: MemberDeclarationSyntax list =
            // A layout that always applies ends the chain: anything after it is unreachable code.
            let guarded, catchAll =
                spec.Layouts
                |> List.map (fun l -> guardCondition s l.Range, l)
                |> List.takeWhile (fun (c, _) -> Option.isSome c)
                |> fun taken ->
                    taken |> List.map (fun (c, l) -> Option.get c, l),
                    spec.Layouts |> List.skip taken.Length |> List.tryHead

            let perVersion =
                [
                    for c, l in guarded do
                        yield! [ sprintf "if (%s)" c; "{" ]
                        yield sprintf "return %s;" (enumSwitchExpr l.Values)
                        yield "}"

                    match catchAll with
                    | Some l -> yield sprintf "return %s;" (enumSwitchExpr l.Values)
                    | None -> yield "return $\"unknown({Value})\";"
                ]

            [
                MethodDeclaration(ParseTypeName "string", "ToString")
                    .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.OverrideKeyword)
                    .WithExpressionBody(ArrowExpressionClause(ParseExpression(enumSwitchExpr mergedByFirstId)))
                    .WithSemicolonToken(Token SyntaxKind.SemicolonToken)
                :> MemberDeclarationSyntax

                MethodDeclaration(ParseTypeName "string", "ToString")
                    .AddModifiers(Token SyntaxKind.PublicKeyword)
                    .AddParameterListParameters(Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"))
                    .WithBody(parseBody perVersion)
                :> MemberDeclarationSyntax
            ]

        let readCore (l: EnumLayout) =
            match enumBacking s l.Backing with
            | None -> [ todoLine (sprintf "backing %A" l.Backing); throwTodoLine name ]
            | Some p -> [ sprintf "return new %s((int)%s.%s);" name s.ReaderParam p.ReadCall ]

        let writeCore (l: EnumLayout) =
            match enumBacking s l.Backing with
            | None -> [ todoLine (sprintf "backing %A" l.Backing); throwTodoLine name ]
            | Some p -> [ sprintf "%s.%s((%s)Value);" s.WriterParam p.WriteMethod p.CsType ]

        let conflictLines =
            [
                for _, v in conflicts -> todoLine (sprintf "%s: value '%s' has different ids per layout" name v)
            ]

        let readBody =
            conflictLines
            @ [ gateLine s name ]
            @ versionedBody s name false [ for l in spec.Layouts -> l.Range, readCore l ]

        let writeBody =
            [ gateLine s name ]
            @ versionedBody s name true [ for l in spec.Layouts -> l.Range, writeCore l ]

        // The model view of a table value is its name; an id the table does not know keeps
        // the `unknown(n)` spelling `ToString` already gives it.
        let jsonBody =
            [ sprintf "%s.WriteStringValue(ToString());" s.JsonWriterParam ]

        let shell =
            (recordStructShell s.ProtocolInterface name [ "int", "Value" ])
                .AddMembers(List.toArray (constants @ conversions))
                .AddMembers(
                    readMethod s name (parseBody readBody),
                    writeMethod s true (parseBody writeBody),
                    writeJsonMethod s true (parseBody jsonBody)
                )
                .AddMembers(List.toArray toStringMembers)
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        // CA2225 wants a named twin for every conversion operator (`ToInt32`, `FromDifficulty`).
        // The named form is already the type's whole surface — `Value` reads the int out and the
        // primary constructor puts one back — so the analyzer would only buy duplicate spellings.
        "#pragma warning disable CA2225\n\n"
        + renderUnit s s.Namespace name [ s.UsingAttributes; s.UsingSerialization; s.UsingJson ] shell
