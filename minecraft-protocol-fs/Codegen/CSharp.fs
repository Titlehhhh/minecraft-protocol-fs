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

    // ----- small text helpers -----

    let private camel (s: string) =
        if s.Length = 0 || s.[0] = '_' then
            s
        else
            string (System.Char.ToLower s.[0]) + s.[1..]

    let private pascal (s: string) =
        if s.Length = 0 then
            s
        else
            string (System.Char.ToUpper s.[0]) + s.[1..]

    /// Comments must stay one line — `%A` of a nested entry may print across several.
    let private oneLine (s: string) = s.Replace("\r", " ").Replace("\n", " ")

    let private todoLine (what: string) =
        sprintf "// TODO(codegen): %s" (oneLine what)

    /// Value types become `record struct`; everything else a `sealed class` (mirrors McProtoNet).
    let private isValue =
        function
        | TBool
        | TInt
        | TLong
        | TFloat
        | TDouble
        | TUuid -> true
        | _ -> false

    /// A version range as a C# boolean condition, or None when it always applies.
    let private guardCondition (s: RuntimeSurface) (range: VersionRange) : string option =
        match VersionRangeX.bounds range with
        | None, None -> None
        | Some lo, None -> Some(sprintf "%s >= %d" s.VersionParam lo)
        | None, Some hi -> Some(sprintf "%s <= %d" s.VersionParam hi)
        | Some lo, Some hi -> Some(sprintf "%s >= %d && %s <= %d" s.VersionParam lo s.VersionParam hi)

    let private gateLine (s: RuntimeSurface) (typeName: string) =
        sprintf "%s<%s>(%s);" s.ThrowIfNotSupported typeName s.VersionParam

    let private throwNoLayoutLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s has no wire layout for protocol version {%s}.\");"
            typeName
            s.VersionParam

    /// Local variable name for an api field; dodges the generated method's own parameter names.
    let private localName (s: RuntimeSurface) (api: string) =
        let n = camel api

        if n = s.VersionParam || n = s.ReaderParam || n = s.WriterParam then
            n + "_"
        else
            n

    // ----- structure: Roslyn building blocks -----

    /// `[ProtocolSupport(from, to)]` from the union of a set of version ranges.
    let private supportAttr (s: RuntimeSurface) (ranges: VersionRange list) : AttributeListSyntax =
        let lo, hi = VersionRangeX.span (if List.isEmpty ranges then [ All ] else ranges)
        let loS = lo |> Option.map string |> Option.defaultValue s.StartProtocolConst
        let hiS = hi |> Option.map string |> Option.defaultValue s.LatestProtocolConst

        AttributeList(
            SingletonSeparatedList(
                Attribute(ParseName s.SupportAttribute)
                    .WithArgumentList(ParseAttributeArgumentList(sprintf "(%s, %s)" loS hiS))
            )
        )

    /// Parse statement / `// comment` lines into a method body. Roslyn re-indents at the end.
    let private parseBody (lines: string list) : BlockSyntax =
        let text = "{\n" + String.concat "\n" lines + "\n}"

        match ParseStatement text with
        | :? BlockSyntax as b -> b
        | other -> failwithf "codegen: body did not parse as a block:\n%O" other

    /// `public static T Read(ref Reader reader, int protocolVersion)`
    let private readMethod (s: RuntimeSurface) (typeName: string) (body: BlockSyntax) : MemberDeclarationSyntax =
        MethodDeclaration(ParseTypeName typeName, s.ReadMethodName)
            .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
            .AddParameterListParameters(
                Parameter(Identifier s.ReaderParam)
                    .WithType(ParseTypeName s.ReaderType)
                    .AddModifiers(Token SyntaxKind.RefKeyword),
                Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int")
            )
            .WithBody(body)

    /// `public [readonly] void Write(Writer writer, int protocolVersion)`
    let private writeMethod (s: RuntimeSurface) (readonlyValue: bool) (body: BlockSyntax) : MemberDeclarationSyntax =
        let modifiers =
            if readonlyValue then
                [| Token SyntaxKind.PublicKeyword; Token SyntaxKind.ReadOnlyKeyword |]
            else
                [| Token SyntaxKind.PublicKeyword |]

        MethodDeclaration(PredefinedType(Token SyntaxKind.VoidKeyword), s.WriteMethodName)
            .AddModifiers(modifiers)
            .AddParameterListParameters(
                Parameter(Identifier s.WriterParam).WithType(ParseTypeName s.WriterType),
                Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int")
            )
            .WithBody(body)

    let private baseTypesFor (s: RuntimeSurface) (name: string) : BaseTypeSyntax[] =
        match s.ProtocolInterface with
        | Some i -> [| SimpleBaseType(ParseTypeName(sprintf "%s<%s>" i name)) :> BaseTypeSyntax |]
        | None -> [||]

    /// `public readonly partial record struct Name(T A, U B) : IProtocolType<Name> { ... }`
    let private recordStructShell
        (s: RuntimeSurface)
        (name: string)
        (positional: (string * string) list)
        : TypeDeclarationSyntax
        =
        let ps =
            positional
            |> List.map (fun (typ, pname) -> Parameter(Identifier pname).WithType(ParseTypeName typ))
            |> List.toArray

        RecordDeclaration(SyntaxKind.RecordStructDeclaration, Token SyntaxKind.RecordKeyword, Identifier name)
            .WithClassOrStructKeyword(Token SyntaxKind.StructKeyword)
            .AddModifiers(
                Token SyntaxKind.PublicKeyword,
                Token SyntaxKind.ReadOnlyKeyword,
                Token SyntaxKind.PartialKeyword
            )
            .AddParameterListParameters(ps)
            .AddBaseListTypes(baseTypesFor s name)
            .WithOpenBraceToken(Token SyntaxKind.OpenBraceToken)
            .WithCloseBraceToken(Token SyntaxKind.CloseBraceToken)
        :> TypeDeclarationSyntax

    /// `public sealed partial class Name : IProtocolType<Name> { get-only props + constructor }`
    let private classShell (s: RuntimeSurface) (name: string) (fields: (string * string) list) : TypeDeclarationSyntax =
        let props: MemberDeclarationSyntax list =
            [
                for typ, fname in fields ->
                    PropertyDeclaration(ParseTypeName typ, fname)
                        .AddModifiers(Token SyntaxKind.PublicKeyword)
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(Token SyntaxKind.SemicolonToken)
                        )
            ]

        let ctor: MemberDeclarationSyntax =
            ConstructorDeclaration(Identifier name)
                .AddModifiers(Token SyntaxKind.PublicKeyword)
                .AddParameterListParameters(
                    [|
                        for typ, fname in fields -> Parameter(Identifier(camel fname)).WithType(ParseTypeName typ)
                    |]
                )
                .WithBody(parseBody [ for _, fname in fields -> sprintf "%s = %s;" fname (camel fname) ])

        ClassDeclaration(name)
            .AddModifiers(
                Token SyntaxKind.PublicKeyword,
                Token SyntaxKind.SealedKeyword,
                Token SyntaxKind.PartialKeyword
            )
            .AddBaseListTypes(baseTypesFor s name)
            .AddMembers(List.toArray (props @ [ ctor ]))
        :> TypeDeclarationSyntax

    /// Assemble usings + file-scoped namespace + one type into formatted, *validated* source text.
    let private renderUnit
        (s: RuntimeSurface)
        (ns: string)
        (label: string)
        (usingNames: string list)
        (decl: MemberDeclarationSyntax)
        : string
        =
        let cu =
            CompilationUnit()
                .AddUsings([| for u in usingNames -> UsingDirective(ParseName u) |])
                .AddMembers(FileScopedNamespaceDeclaration(ParseName ns).AddMembers(decl))
                .NormalizeWhitespace("    ", "\n", false)

        let errors =
            cu.GetDiagnostics()
            |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
            |> Seq.toList

        if not errors.IsEmpty then
            failwithf
                "codegen: emitted invalid C# for %s:\n%s\n----\n%s"
                label
                (errors |> List.map string |> String.concat "\n")
                (cu.ToFullString())

        cu.ToFullString() + "\n"

    // ----- wire entries -> statements -----

    /// C# type of a wire item when held in a local or array (primitives + named types only).
    let private itemCsType (s: RuntimeSurface) (w: WireType) : string option =
        match w with
        | Named n -> Some n
        | _ -> s.Primitives.TryFind w |> Option.map (fun p -> p.CsType)

    /// Count-prefix read: setup lines + the count expression.
    let private countRead (s: RuntimeSurface) (cnt: ArrayCount) (ln: string) : (string list * string) option =
        match cnt with
        | VarIntCount -> Some([ sprintf "int %sCount = %s.ReadVarInt();" ln s.ReaderParam ], sprintf "%sCount" ln)
        | FixedCount n -> Some([], string n)
        | TypedCount w ->
            s.Primitives.TryFind w
            |> Option.map (fun p ->
                [ sprintf "int %sCount = checked((int)%s.%s);" ln s.ReaderParam p.ReadCall ], sprintf "%sCount" ln)

    /// Count-prefix write lines for a value's `.Length`.
    let private countWrite (s: RuntimeSurface) (cnt: ArrayCount) (value: string) : string list option =
        match cnt with
        | VarIntCount -> Some [ sprintf "%s.WriteVarInt(%s.Length);" s.WriterParam value ]
        | FixedCount _ -> Some []
        | TypedCount w ->
            s.Primitives.TryFind w
            |> Option.map (fun p -> [ sprintf "%s.%s((%s)%s.Length);" s.WriterParam p.WriteMethod p.CsType value ])

    /// One wire entry -> read statement lines.
    let private readEntryLines (s: RuntimeSurface) (entry: WireEntry) : Result<string list, string> =
        match entry with
        | Read(_, Option inner, api) ->
            let ln = localName s api

            match readExpr s inner, itemCsType s inner with
            | Ok call, Some t ->
                Ok
                    [
                        sprintf "%s? %s = null;" t ln
                        sprintf "if (%s.ReadBoolean()) %s = %s;" s.ReaderParam ln call
                    ]
            | _ -> Error(sprintf "read '%s' (Option %A)" api inner)
        | Read(_, Array(item, cnt), api) ->
            let ln = localName s api

            match readExpr s item, itemCsType s item, countRead s cnt ln with
            | Ok call, Some t, Some(setup, cntExpr) ->
                Ok(
                    setup
                    @ [
                        sprintf "var %s = new %s[%s];" ln t cntExpr
                        sprintf "for (int i = 0; i < %s.Length; i++) %s[i] = %s;" ln ln call
                    ]
                )
            | _ -> Error(sprintf "read '%s' (Array %A)" api item)
        | Read(_, wt, api) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "var %s = %s;" (localName s api) call ]
            | Error e -> Error(sprintf "read '%s' (%s)" api e)
        | Discard(wire, Option inner) ->
            match readExpr s inner with
            | Ok call -> Ok [ sprintf "if (%s.ReadBoolean()) %s;" s.ReaderParam call ]
            | Error e -> Error(sprintf "discard '%s' (Option: %s)" wire e)
        | Discard(wire, Array(item, cnt)) ->
            let ln = "skip" + pascal (camel wire)

            match readExpr s item, countRead s cnt ln with
            | Ok call, Some(setup, cntExpr) -> Ok(setup @ [ sprintf "for (int i = 0; i < %s; i++) %s;" cntExpr call ])
            | _ -> Error(sprintf "discard '%s' (Array %A)" wire item)
        | Discard(wire, wt) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | other -> Error(sprintf "%A" other)

    /// One wire entry -> write statement lines. `apiTypes` drives narrowing casts.
    let private writeEntryLines
        (s: RuntimeSurface)
        (apiTypes: Map<string, ApiType>)
        (entry: WireEntry)
        : Result<string list, string>
        =
        match entry with
        | Read(_, _, api) when api.StartsWith "_" ->
            // wire-only discriminator: its value must be derived from the model, which needs the
            // union/conditional entry that consumes it to be generated first
            Error(sprintf "write wire-only '%s' (derive from model)" api)
        | Read(_, Option inner, api) ->
            let v = camel api + "Value"

            match writeExpr s inner None v with
            | Ok call ->
                Ok
                    [
                        sprintf "%s.WriteBoolean(%s is not null);" s.WriterParam api
                        sprintf "if (%s is { } %s) %s;" api v call
                    ]
            | Error e -> Error(sprintf "write '%s' (Option: %s)" api e)
        | Read(_, Array(item, cnt), api) ->
            let iv = camel api + "Item"

            match writeExpr s item None iv, countWrite s cnt api with
            | Ok call, Some cw -> Ok(cw @ [ sprintf "foreach (var %s in %s) %s;" iv api call ])
            | _ -> Error(sprintf "write '%s' (Array %A)" api item)
        | Read(_, wt, api) ->
            // an option-typed api field written as a required wire value must be present
            let apiT = apiTypes.TryFind api

            let requiredT =
                match apiT with
                | Some(TOption t) -> Some t
                | other -> other

            let value =
                match apiT with
                | Some(TOption _) ->
                    sprintf
                        "(%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\"))"
                        api
                        api
                | _ -> api
            // narrow explicitly when the api type is wider than the wire primitive (int api, i8 wire)
            let cast =
                match s.Primitives.TryFind wt, requiredT with
                | Some p, Some t when csType s t <> p.CsType -> Some p.CsType
                | _ -> None

            match writeExpr s wt cast value with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "write '%s' (%s)" api e)
        | Discard(_, Option _) -> Ok [ sprintf "%s.WriteBoolean(false);" s.WriterParam ]
        | Discard(wire, Array(_, cnt)) ->
            match cnt with
            | VarIntCount -> Ok [ sprintf "%s.WriteVarInt(0);" s.WriterParam ]
            | TypedCount w ->
                match s.Primitives.TryFind w with
                | Some p -> Ok [ sprintf "%s.%s(0);" s.WriterParam p.WriteMethod ]
                | None -> Error(sprintf "discard '%s' (Array count %A)" wire w)
            | FixedCount _ -> Error(sprintf "discard '%s' (fixed-count array needs real items)" wire)
        | Discard(wire, wt) ->
            let value =
                match wt with
                | Str -> "\"\""
                | _ -> "default"

            match writeExpr s wt None value with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | other -> Error(sprintf "%A" other)

    // ----- per-layout bodies + version branching -----

    let private layoutReadLines
        (s: RuntimeSurface)
        (name: string)
        (apiFields: ApiField list)
        (l: WireLayout)
        : string list
        =
        let results = l.Entries |> List.map (fun e -> e, readEntryLines s e)

        let bound =
            results
            |> List.choose (function
                | Read(_, _, api), Ok _ -> Some(localName s api)
                | _ -> None)
            |> Set.ofList

        let lines =
            results
            |> List.collect (function
                | _, Ok ls -> ls
                | _, Error e -> [ todoLine e ])

        let ctorArgs =
            apiFields
            |> List.map (fun f ->
                if bound.Contains(localName s f.Name) then
                    localName s f.Name
                else
                    "default!")
            |> String.concat ", "

        lines @ [ sprintf "return new %s(%s);" name ctorArgs ]

    let private layoutWriteLines (s: RuntimeSurface) (apiTypes: Map<string, ApiType>) (l: WireLayout) : string list =
        l.Entries
        |> List.collect (fun e ->
            match writeEntryLines s apiTypes e with
            | Ok ls -> ls
            | Error err -> [ todoLine err ])

    /// One guarded branch per layout. A single unconditional layout stays flat (the support
    /// attribute already gates its span); with several layouts, a version that matches none throws.
    let private versionedBody
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
                    let body = if isWrite then body @ [ "return;" ] else body

                    match guardCondition s r with
                    | Some c -> yield! [ sprintf "if (%s)" c; "{" ] @ body @ [ "}" ]
                    | None -> yield! [ "{" ] @ body @ [ "}" ]
            ]
            @ (if hasCatchAll then [] else [ throwNoLayoutLine s typeName ])

    // ----- named types -----

    let private usingsFor (s: RuntimeSurface) (fields: ApiField list) =
        let text = fields |> List.map (fun f -> csType s f.Type) |> String.concat " "

        [
            yield s.UsingAttributes
            yield s.UsingSerialization
            if text.Contains s.NbtType then
                yield s.UsingNbt
            if text.Contains s.UuidType then
                yield s.UsingSystem
        ]

    let private renderType (s: RuntimeSurface) (ns: string) (spec: NamedTypeSpec) : string =
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
            :: versionedBody s spec.Name true [ for l in spec.Layouts -> l.Range, layoutWriteLines s apiTypes l ]

        let shell =
            if value then
                recordStructShell s spec.Name fields
            else
                classShell s spec.Name fields

        let shell =
            shell
                .AddMembers(readMethod s spec.Name (parseBody readBody), writeMethod s value (parseBody writeBody))
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        renderUnit s ns spec.Name (usingsFor s spec.ApiFields) shell

    // ----- packets -----

    /// Packets live under `<root>.Packets.<State>.<Direction>` so same-named packets from
    /// different states/directions (e.g. `KeepAlivePacket` in Configuration vs Play) don't collide.
    let private packetNamespace (s: RuntimeSurface) (p: PacketSpec) : string =
        sprintf "%s.Packets.%A.%A" s.Namespace p.State p.Direction

    /// A packet renders exactly like a named type; only the namespace and file placement differ.
    let private renderPacket (s: RuntimeSurface) (p: PacketSpec) : string =
        renderType
            s
            (packetNamespace s p)
            {
                Name = p.ClassName
                ApiFields = p.ApiFields
                Layouts = p.Layouts
            }

    // ----- bitflags -----

    let private renderBitflags (s: RuntimeSurface) (spec: BitflagsSpec) : string =
        let name = spec.Name
        let apiFlags = spec.Layouts |> List.collect (fun l -> l.Flags) |> List.distinct

        let readCore (l: BitflagsLayout) =
            match integralBacking s l.Backing with
            | None -> [ todoLine (sprintf "backing %A" l.Backing); "return default;" ]
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
            | None -> [ todoLine (sprintf "backing %A" l.Backing) ]
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

        let shell =
            (recordStructShell s name (apiFlags |> List.map (fun f -> "bool", pascal f)))
                .AddMembers(readMethod s name (parseBody readBody), writeMethod s true (parseBody writeBody))
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        renderUnit s s.Namespace name [ s.UsingAttributes; s.UsingSerialization ] shell

    // ----- target -----

    /// A C# code-generation target bound to a specific runtime surface.
    let targetFor (surface: RuntimeSurface) : ILanguageTarget =
        { new ILanguageTarget with
            member _.Id = "csharp"
            member _.Extension = ".cs"
            member _.RenderType spec = renderType surface surface.Namespace spec
            member _.RenderBitflags spec = renderBitflags surface spec
            member _.RenderPacket spec = renderPacket surface spec
        }

    /// The default C# target: the McProtoNet runtime surface.
    let target: ILanguageTarget = targetFor mcProtoNet
