namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Unions =

    open type SyntaxFactory
    open Text
    open Structure
    open Statements
    open Bodies
    open PacketLayers
    open Json

    // ----- unions -----

    /// One case of a rendered union: the nested record's name and its positional parameters.
    /// `ArmName` is the DSL name the JSON view labels the case with, whatever suffix the C#
    /// case carries; `JsonFields` are the same parameters with their JSON shape.
    type private UnionCase =
        {
            CaseName: string
            Params: (string * string) list
            ArmName: string
            JsonFields: (string * JsonShape) list
        }

    /// Positional parameters of an arm, in wire order. The parameter is named by the api name
    /// verbatim, because `writeEntryLines` addresses a value by that same string: any second
    /// spelling here (Pascal-casing, say) generates a write body that references a local nobody
    /// declared. The packet path aliases layer fields under the api name for the same reason.
    /// An entry whose shape has no renderer drops out here — the same entry also fails
    /// `readEntryLines`, so that arm's bodies become stubs while the case still declares and the
    /// file still compiles.
    let private armParams (s: RuntimeSurface) (arm: UnionArm) : (string * string) list =
        arm.Entries
        |> List.choose (function
            | Read(_, w, api) when not (api.StartsWith "_") -> wireCsType s w |> Option.map (fun t -> t, api)
            | _ -> None)

    /// The JSON shape of each positional parameter, in `armParams` order: an entry drops out of
    /// both lists for the same reason, so the two stay aligned.
    let private armJsonFields (s: RuntimeSurface) (arm: UnionArm) : (string * JsonShape) list =
        arm.Entries
        |> List.choose (function
            | Read(_, w, api) when not (api.StartsWith "_") ->
                match wireCsType s w, shapeOfWire s w with
                | Some _, Some shape -> Some(api, shape)
                | _ -> None
            | _ -> None)

    /// Case name of an arm inside one layer: the DSL name while the arm's parameter list is the
    /// same in every layer that carries it, else the name plus the layer label. TeamAction's
    /// `Created` is string-typed until 764 and NBT-typed from 771 — one record cannot be both,
    /// so both shapes exist as separate cases.
    let private unionCaseName (s: RuntimeSurface) (spec: UnionTypeSpec) (l: UnionLayout) (arm: UnionArm) : string =
        let shapes =
            spec.Layouts
            |> List.choose (fun other -> other.Arms |> List.tryFind (fun a -> a.Name = arm.Name))
            |> List.map (armParams s)
            |> List.distinct

        if List.length shapes <= 1 then
            arm.Name
        else
            arm.Name + layerName (spec.Layouts |> List.map (fun x -> x.Range)) l.Range

    /// Every case the union declares, across all layers, deduplicated by name.
    let private unionCases (s: RuntimeSurface) (spec: UnionTypeSpec) : UnionCase list =
        [
            for l in spec.Layouts do
                for a in l.Arms ->
                    {
                        CaseName = unionCaseName s spec l a
                        Params = armParams s a
                        ArmName = a.Name
                        JsonFields = armJsonFields s a
                    }
        ]
        |> List.distinctBy (fun c -> c.CaseName)

    /// Read body of one layer: switch on the discriminator the container already took off the
    /// wire, read that arm's entries into locals, construct its case. Each arm gets its own block
    /// so two arms may bind the same local name.
    let private unionReadLines (s: RuntimeSurface) (spec: UnionTypeSpec) (l: UnionLayout) : string list =
        [
            yield sprintf "switch (%s)" s.DiscriminatorParam
            yield "{"

            for arm in l.Arms do
                for k in arm.Keys do
                    yield sprintf "case %d:" k

                yield "{"

                let results = readEntriesLines s arm.Entries

                let errors =
                    results
                    |> List.choose (function
                        | _, Error e -> Some e
                        | _ -> None)

                if not (List.isEmpty errors) then
                    yield! errors |> List.map todoLine
                    yield throwTodoLine spec.Name
                else
                    yield!
                        results
                        |> List.collect (function
                            | _, Ok ls -> ls
                            | _, Error _ -> [])

                    let args =
                        arm.Entries
                        |> List.choose (function
                            | Read(_, _, api) when not (api.StartsWith "_") -> Some(localName s api)
                            | _ -> None)

                    yield sprintf "return new %s(%s);" (unionCaseName s spec l arm) (String.concat ", " args)

                yield "}"

            yield "}"
            yield throwNoCaseLine s spec.Name
        ]

    /// Write body of one layer: switch on the concrete case, then write that arm's entries. The
    /// entry renderer addresses values by their api name, so each case property is aliased to a
    /// local of exactly that name (the same trick form-A layers use).
    let private unionWriteLines (s: RuntimeSurface) (spec: UnionTypeSpec) (l: UnionLayout) : string list =
        [
            yield "switch (this)"
            yield "{"

            for arm in l.Arms do
                let ps = armParams s arm

                yield sprintf "case %s %s:" (unionCaseName s spec l arm) (if ps.IsEmpty then "_" else "arm")
                yield "{"

                let results =
                    arm.Entries |> List.map (writeEntryLines s (writeCtxOf Map.empty arm.Entries))

                let errors =
                    results
                    |> List.choose (function
                        | Error e -> Some e
                        | _ -> None)

                if not (List.isEmpty errors) then
                    yield! errors |> List.map todoLine
                    yield throwTodoLine spec.Name
                else
                    for t, n in ps do
                        yield sprintf "%s %s = arm.%s;" t n n

                    yield!
                        results
                        |> List.collect (function
                            | Ok ls -> ls
                            | Error _ -> [])

                    yield "return;"

                yield "}"

            yield "}"
            yield throwNoCaseLayerLine s spec.Name
        ]

    /// JSON body: one object, the case named under the surface's case property, then the case's
    /// own fields. Version-free — the case the value holds is the whole story.
    let private unionJsonLines (s: RuntimeSurface) (spec: UnionTypeSpec) (cases: UnionCase list) : string list =
        let w = s.JsonWriterParam

        [
            yield sprintf "%s.WriteStartObject();" w
            yield "switch (this)"
            yield "{"

            for c in cases do
                yield sprintf "case %s %s:" c.CaseName (if c.JsonFields.IsEmpty then "_" else "arm")
                yield "{"
                yield sprintf "%s.WriteString(\"%s\", \"%s\");" w s.UnionCaseProperty c.ArmName

                for n, shape in c.JsonFields do
                    yield! propertyLines s n shape (sprintf "arm.%s" n)

                yield "break;"
                yield "}"

            yield "default:"
            yield sprintf "throw new System.NotSupportedException($\"%s case {GetType().Name} has no JSON view.\");" spec.Name

            yield "}"
            yield sprintf "%s.WriteEndObject();" w
        ]

    /// `Discriminator` body of one layer: the case picks the key. An arm that reads under several
    /// keys (`case [3; 4] "PlayersChanged"`) writes the first one.
    let private unionDiscriminatorLines (s: RuntimeSurface) (spec: UnionTypeSpec) (l: UnionLayout) : string list =
        [
            yield "switch (this)"
            yield "{"

            for arm in l.Arms do
                match arm.Keys with
                | key :: _ -> yield sprintf "case %s _: return %d;" (unionCaseName s spec l arm) key
                | [] -> ()

            yield "}"
            yield throwNoCaseLayerLine s spec.Name
        ]

    /// `[Union] public partial record Name { ... }`. Not sealed and not an `IProtocolType`: the
    /// source generator derives the case records from the nested partials, and the read needs the
    /// discriminator the containing layout already consumed, which that interface has no room for.
    let private unionShell (name: string) : TypeDeclarationSyntax =
        RecordDeclaration(SyntaxKind.RecordDeclaration, Token SyntaxKind.RecordKeyword, Identifier name)
            .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.PartialKeyword)
            .WithOpenBraceToken(Token SyntaxKind.OpenBraceToken)
            .WithCloseBraceToken(Token SyntaxKind.CloseBraceToken)
        :> TypeDeclarationSyntax

    let private unionAttr (s: RuntimeSurface) : AttributeListSyntax =
        AttributeList(SingletonSeparatedList(Attribute(ParseName s.UnionAttribute)))

    /// `public static T Read(ref Reader reader, int protocolVersion, int discriminator)`.
    let private unionReadMethod (s: RuntimeSurface) (typeName: string) (body: BlockSyntax) : MemberDeclarationSyntax =
        MethodDeclaration(ParseTypeName typeName, s.ReadMethodName)
            .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
            .AddParameterListParameters(
                Parameter(Identifier s.ReaderParam)
                    .WithType(ParseTypeName s.ReaderType)
                    .AddModifiers(Token SyntaxKind.RefKeyword),
                Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"),
                Parameter(Identifier s.DiscriminatorParam).WithType(ParseTypeName "int")
            )
            .WithBody(body)

    /// `public int Discriminator(int protocolVersion)` — the key the containing layout writes
    /// ahead of the union body, derived from the case the model holds.
    let private discriminatorMethod (s: RuntimeSurface) (body: BlockSyntax) : MemberDeclarationSyntax =
        MethodDeclaration(PredefinedType(Token SyntaxKind.IntKeyword), s.DiscriminatorMethodName)
            .AddModifiers(Token SyntaxKind.PublicKeyword)
            .AddParameterListParameters(Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"))
            .WithBody(body)

    let private unionUsings (s: RuntimeSurface) (cases: UnionCase list) =
        let text =
            cases |> List.collect (fun c -> c.Params |> List.map fst) |> String.concat " "

        [
            yield s.UsingUnion
            yield s.UsingAttributes
            yield s.UsingSerialization
            yield s.UsingJson
            if text.Contains s.NbtType then
                yield s.UsingNbt
            if text.Contains s.UuidType then
                yield s.UsingSystem
        ]

    /// A case record shadows a same-named type for the whole union body: read as written,
    /// `Rotations(Rotations Value)` binds the parameter to the case, not to the type. Rewriting
    /// the wire's named references to namespace-qualified ones fixes every use at once — the case
    /// declarations, the `ReadType<T>` calls and the write-side locals all come from these entries.
    /// The shadow set is every arm name, whether or not the layer label ends up suffixed to it: an
    /// unnecessary qualification is only longer, a missing one is wrong code.
    let private qualifyShadowed (s: RuntimeSurface) (spec: UnionTypeSpec) : UnionTypeSpec =
        let shadowed =
            spec.Layouts
            |> List.collect (fun l -> l.Arms |> List.map (fun a -> a.Name))
            |> Set.ofList

        let rec wire w =
            match w with
            | Named n when shadowed.Contains n -> Named(sprintf "%s.%s" s.Namespace n)
            | Array(item, cnt) -> Array(wire item, cnt)
            | Option inner -> Option(wire inner)
            | SentinelArray(item, e) -> SentinelArray(wire item, e)
            | RegistryHolder inner -> RegistryHolder(wire inner)
            | other -> other

        let entry e =
            match e with
            | Read(w, t, api) -> Read(w, wire t, api)
            | Discard(w, t) -> Discard(w, wire t)
            | other -> other

        { spec with
            Layouts =
                [
                    for l in spec.Layouts ->
                        { l with
                            Arms =
                                [
                                    for a in l.Arms ->
                                        { a with
                                            Entries = a.Entries |> List.map entry
                                        }
                                ]
                        }
                ]
        }

    let internal renderUnion (s: RuntimeSurface) (spec: UnionTypeSpec) : string =
        let spec = qualifyShadowed s spec
        let cases = unionCases s spec

        let caseDecls: MemberDeclarationSyntax list =
            [
                for c in cases ->
                    ParseMemberDeclaration(
                        sprintf
                            "partial record %s(%s);"
                            c.CaseName
                            (c.Params |> List.map (fun (t, n) -> sprintf "%s %s" t n) |> String.concat ", ")
                    )
            ]

        let readBody =
            gateLine s spec.Name
            :: versionedBody s spec.Name false [ for l in spec.Layouts -> l.Range, unionReadLines s spec l ]

        let writeBody =
            gateLine s spec.Name
            :: versionedBody s spec.Name true [ for l in spec.Layouts -> l.Range, unionWriteLines s spec l ]

        let discBody =
            versionedBody s spec.Name false [ for l in spec.Layouts -> l.Range, unionDiscriminatorLines s spec l ]

        let shell =
            (unionShell spec.Name)
                .AddMembers(List.toArray caseDecls)
                .AddMembers(
                    unionReadMethod s spec.Name (parseBody readBody),
                    writeMethod s false (parseBody writeBody),
                    discriminatorMethod s (parseBody discBody),
                    writeJsonMethod s false (parseBody (unionJsonLines s spec cases))
                )
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))
                .AddAttributeLists(unionAttr s)

        renderUnit s s.Namespace spec.Name (unionUsings s cases) shell
