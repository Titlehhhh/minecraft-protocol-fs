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

    /// C# reserved keywords; a camel-cased identifier that collides with one must be verbatim
    /// (`@namespace`), else Roslyn emits the bare keyword and the file fails to parse.
    let private csharpKeywords =
        set [
            "abstract"; "as"; "base"; "bool"; "break"; "byte"; "case"; "catch"; "char"; "checked"
            "class"; "const"; "continue"; "decimal"; "default"; "delegate"; "do"; "double"; "else"
            "enum"; "event"; "explicit"; "extern"; "false"; "finally"; "fixed"; "float"; "for"
            "foreach"; "goto"; "if"; "implicit"; "in"; "int"; "interface"; "internal"; "is"; "lock"
            "long"; "namespace"; "new"; "null"; "object"; "operator"; "out"; "override"; "params"
            "private"; "protected"; "public"; "readonly"; "ref"; "return"; "sbyte"; "sealed"
            "short"; "sizeof"; "stackalloc"; "static"; "string"; "struct"; "switch"; "this"; "throw"
            "true"; "try"; "typeof"; "uint"; "ulong"; "unchecked"; "unsafe"; "ushort"; "using"
            "virtual"; "void"; "volatile"; "while"
        ]

    let private camel (s: string) =
        let n =
            if s.Length = 0 || s.[0] = '_' then
                s
            else
                string (System.Char.ToLower s.[0]) + s.[1..]

        if csharpKeywords.Contains n then "@" + n else n

    let private pascal (s: string) =
        if s.Length = 0 then
            s
        else
            string (System.Char.ToUpper s.[0]) + s.[1..]

    /// Comments must stay one line — `%A` of a nested entry may print across several.
    let private oneLine (s: string) = s.Replace("\r", " ").Replace("\n", " ")

    let private todoLine (what: string) =
        sprintf "// TODO(codegen): %s" (oneLine what)

    /// A stub branch must fail loudly at runtime: silently reading/writing a partial wire
    /// (the pre-2026-08-01 behaviour) corrupts the stream for that protocol version.
    let private throwTodoLine (typeName: string) =
        sprintf
            "throw new System.NotImplementedException(\"TODO(codegen): %s wire layout is not fully generated for this protocol version.\");"
            typeName

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

    let private baseTypesFor (iface: string option) (name: string) : BaseTypeSyntax[] =
        match iface with
        | Some i -> [| SimpleBaseType(ParseTypeName(sprintf "%s<%s>" i name)) :> BaseTypeSyntax |]
        | None -> [||]

    /// `public readonly partial record struct Name(T A, U B) : IProtocolType<Name> { ... }`
    let private recordStructShell
        (iface: string option)
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
            .AddBaseListTypes(baseTypesFor iface name)
            .WithOpenBraceToken(Token SyntaxKind.OpenBraceToken)
            .WithCloseBraceToken(Token SyntaxKind.CloseBraceToken)
        :> TypeDeclarationSyntax

    /// `public sealed partial class Name : IProtocolType<Name> { get-only props + constructor }`
    let private classShell
        (iface: string option)
        (name: string)
        (fields: (string * string) list)
        : TypeDeclarationSyntax
        =
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
            .AddBaseListTypes(baseTypesFor iface name)
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

        let errors = results |> List.choose (function _, Error e -> Some e | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            let bound =
                results
                |> List.choose (function
                    | Read(_, _, api), Ok _ -> Some(localName s api)
                    | _ -> None)
                |> Set.ofList

            let lines = results |> List.collect (function _, Ok ls -> ls | _, Error _ -> [])

            let ctorArgs =
                apiFields
                |> List.map (fun f ->
                    if bound.Contains(localName s f.Name) then
                        localName s f.Name
                    else
                        "default!")
                |> String.concat ", "

            lines @ [ sprintf "return new %s(%s);" name ctorArgs ]

    let private layoutWriteLines
        (s: RuntimeSurface)
        (name: string)
        (apiTypes: Map<string, ApiType>)
        (l: WireLayout)
        : string list
        =
        let results = l.Entries |> List.map (writeEntryLines s apiTypes)

        let errors = results |> List.choose (function Error e -> Some e | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            results |> List.collect (function Ok ls -> ls | Error _ -> [])

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
                    let endsWithThrow =
                        body |> List.tryLast |> Option.exists (fun (l: string) -> l.StartsWith "throw")

                    let body = if isWrite && not endsWithThrow then body @ [ "return;" ] else body

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

    let private renderType
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
                [
                    for l in spec.Layouts -> l.Range, layoutWriteLines s spec.Name apiTypes l
                ]

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

    // ----- packets -----

    /// Packets live under `<root>.Packets.<State>.<Direction>` so same-named packets from
    /// different states/directions (e.g. `KeepAlivePacket` in Configuration vs Play) don't collide.
    let private packetNamespace (s: RuntimeSurface) (p: PacketSpec) : string =
        sprintf "%s.Packets.%A.%A" s.Namespace p.State p.Direction

    /// Manifest id ranges, ascending, with adjacent ranges carrying the same id merged
    /// (755–755 + 756–756 + 757–758 @ 0x21 -> 755–758 @ 0x21).
    let private coalesceIds (ids: (int * int * int) list) : (int * int * int) list =
        ids
        |> List.sortBy (fun (lo, _, _) -> lo)
        |> List.fold
            (fun acc (lo, hi, id) ->
                match acc with
                | (plo, phi, pid) :: rest when pid = id && phi + 1 = lo -> (plo, hi, pid) :: rest
                | _ -> (lo, hi, id) :: acc)
            []
        |> List.rev

    /// `public static bool TryGetPacketId(int protocolVersion, out int id)`: one guarded branch
    /// per coalesced manifest range, unknown version -> false; plus `GetPacketId` as a throwing
    /// wrapper — both only emitted for packets whose `Ids` the manifest resolved
    /// (see `PacketIds.enrich`).
    let private packetIdMethods (s: RuntimeSurface) (ids: (int * int * int) list) : MemberDeclarationSyntax list =
        let tryBody =
            [
                for lo, hi, id in coalesceIds ids ->
                    sprintf
                        "if (%s >= %d && %s <= %d) { id = 0x%02X; return true; }"
                        s.VersionParam
                        lo
                        s.VersionParam
                        hi
                        id
            ]
            @ [ "id = 0;"; "return false;" ]

        let tryDecl =
            MethodDeclaration(PredefinedType(Token SyntaxKind.BoolKeyword), "TryGetPacketId")
                .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
                .AddParameterListParameters(
                    Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"),
                    Parameter(Identifier "id")
                        .WithType(ParseTypeName "int")
                        .AddModifiers(Token SyntaxKind.OutKeyword)
                )
                .WithBody(parseBody tryBody)

        let getBody =
            [
                sprintf "if (TryGetPacketId(%s, out var id)) return id;" s.VersionParam
                sprintf
                    "throw new System.NotSupportedException($\"No packet id for protocol {%s}.\");"
                    s.VersionParam
            ]

        let getDecl =
            MethodDeclaration(PredefinedType(Token SyntaxKind.IntKeyword), "GetPacketId")
                .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
                .AddParameterListParameters(Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"))
                .WithBody(parseBody getBody)

        [ tryDecl; getDecl ]

    // ----- form A: version groups (layers) -----

    /// One version layer of a form-A packet. `GroupName` is what consumers see as the nullable
    /// property (`V764_Last`); the nested struct type gets a `Layer` suffix (`V764_LastLayer`)
    /// because a member and a nested type cannot share a name (CS0102). `Fields` are the layer's
    /// non-common api fields with their per-layer C# type (nullability from the layer's wire).
    type private PacketLayer =
        {
            Layout: WireLayout
            GroupName: string option
            Fields: (string * string) list
        }

    /// Mechanical group name from a layout range: `V759`, `V761_763`, `V764_Last`.
    let private layerName (allLayouts: WireLayout list) (r: VersionRange) : string =
        match VersionRangeX.bounds r with
        | Some a, Some b when a = b -> sprintf "V%d" a
        | Some a, Some b -> sprintf "V%d_%d" a b
        | Some a, None -> sprintf "V%d_Last" a
        | None, Some b ->
            match VersionRangeX.span (allLayouts |> List.map (fun l -> l.Range)) |> fst with
            | Some lo -> sprintf "V%d_%d" lo b
            | None -> sprintf "VUntil%d" b
        | None, None -> "VAll"

    /// Api field names a layout's entries bind (wire-only `_x` discriminators excluded).
    let rec private boundApis (entries: WireEntry list) : string list =
        entries
        |> List.collect (function
            | Read(_, _, api) when not (api.StartsWith "_") -> [ api ]
            | ReadOpt(_, _, api, _, _) -> [ api ]
            | ReadBlock(_, api, _) -> [ api ]
            | ReadUnion(_, _, api) -> [ api ]
            | IfNonZero(_, inner) -> boundApis inner
            | _ -> [])

    /// A field is common when it lives in every version (`Present = All`).
    let private isCommon (f: ApiField) = f.Present = All

    /// Per-layer C# type of a group field: existence-optionality (`TOption` because the field is
    /// absent in other versions) is stripped; the layer's own wire decides real nullability.
    let private layerFieldType (s: RuntimeSurface) (l: WireLayout) (f: ApiField) : string =
        let inner =
            match f.Type with
            | TOption t -> t
            | t -> t

        let optionalHere =
            l.Entries
            |> List.exists (function
                | Read(_, Option _, api) -> api = f.Name
                | ReadOpt(_, _, api, _, _) -> api = f.Name
                | _ -> false)

        if optionalHere then
            csType s inner + "?"
        else
            csType s inner

    /// Cut a multi-layout packet into layers. A layer with no non-common fields gets no group.
    let private packetLayers (s: RuntimeSurface) (p: PacketSpec) : PacketLayer list =
        let commonNames =
            p.ApiFields |> List.filter isCommon |> List.map (fun f -> f.Name) |> Set.ofList

        [
            for l in p.Layouts ->
                let bound = boundApis l.Entries |> Set.ofList

                let fields =
                    p.ApiFields
                    |> List.filter (fun f -> not (commonNames.Contains f.Name) && bound.Contains f.Name)
                    |> List.map (fun f -> layerFieldType s l f, f.Name)

                {
                    Layout = l
                    GroupName = (if fields.IsEmpty then None else Some(layerName p.Layouts l.Range))
                    Fields = fields
                }
        ]

    /// `public sealed partial record Name(TCommon A, V759Layer? V759 = null, ...) : IPacket<Name>`
    /// with one nested `readonly record struct {G}Layer(...)` per group.
    let private packetRecordShell
        (iface: string option)
        (name: string)
        (common: (string * string) list)
        (layers: PacketLayer list)
        : TypeDeclarationSyntax
        =
        let ps =
            [
                for typ, pname in common do
                    yield Parameter(Identifier pname).WithType(ParseTypeName typ)
                for l in layers do
                    match l.GroupName with
                    | Some g ->
                        // The record header resolves names in the enclosing scope, not inside the
                        // record — nested layer types must be qualified with the packet name.
                        yield
                            Parameter(Identifier g)
                                .WithType(ParseTypeName(sprintf "%s.%sLayer?" name g))
                                .WithDefault(EqualsValueClause(ParseExpression "null"))
                    | None -> ()
            ]

        let nested: MemberDeclarationSyntax list =
            [
                for l in layers do
                    match l.GroupName with
                    | Some g ->
                        ParseMemberDeclaration(
                            sprintf
                                "public readonly record struct %sLayer(%s);"
                                g
                                (l.Fields
                                 |> List.map (fun (t, n) -> sprintf "%s %s" t n)
                                 |> String.concat ", ")
                        )
                    | None -> ()
            ]

        RecordDeclaration(SyntaxKind.RecordDeclaration, Token SyntaxKind.RecordKeyword, Identifier name)
            .AddModifiers(
                Token SyntaxKind.PublicKeyword,
                Token SyntaxKind.SealedKeyword,
                Token SyntaxKind.PartialKeyword
            )
            .AddParameterListParameters(List.toArray ps)
            .AddBaseListTypes(baseTypesFor iface name)
            .WithOpenBraceToken(Token SyntaxKind.OpenBraceToken)
            .WithCloseBraceToken(Token SyntaxKind.CloseBraceToken)
            .AddMembers(List.toArray nested)
        :> TypeDeclarationSyntax

    /// Read body of one layer: entry lines as usual, then a ctor call that fills common
    /// positionally and this layer's group (if any) by named argument.
    let private formAReadLines
        (s: RuntimeSurface)
        (name: string)
        (common: ApiField list)
        (layer: PacketLayer)
        : string list
        =
        let results = layer.Layout.Entries |> List.map (fun e -> e, readEntryLines s e)

        let errors = results |> List.choose (function _, Error e -> Some e | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            let bound =
                results
                |> List.choose (function
                    | Read(_, _, api), Ok _ -> Some(localName s api)
                    | _ -> None)
                |> Set.ofList

            let lines = results |> List.collect (function _, Ok ls -> ls | _, Error _ -> [])

            let commonArgs =
                common
                |> List.map (fun f ->
                    if bound.Contains(localName s f.Name) then
                        localName s f.Name
                    else
                        "default!")

            let groupArg =
                match layer.GroupName with
                | Some g ->
                    let args = layer.Fields |> List.map (fun (_, n) -> localName s n)
                    [ sprintf "%s: new %sLayer(%s)" g g (String.concat ", " args) ]
                | None -> []

            lines @ [ sprintf "return new %s(%s);" name (String.concat ", " (commonArgs @ groupArg)) ]

    /// Write body of one layer. A layer with a group first demands it (`WrongLayerException`),
    /// then aliases each group field as a local with the *api-level* type (keeps the `?? throw`
    /// required-write path valid) under the api name, so entry rendering stays untouched.
    let private formAWriteLines
        (s: RuntimeSurface)
        (name: string)
        (apiTypes: Map<string, ApiType>)
        (layer: PacketLayer)
        : string list
        =
        let results = layer.Layout.Entries |> List.map (writeEntryLines s apiTypes)

        let errors = results |> List.choose (function Error e -> Some e | _ -> None)

        if not (List.isEmpty errors) then
            (errors |> List.map todoLine) @ [ throwTodoLine name ]
        else
            let unpack =
                match layer.GroupName with
                | Some g ->
                    sprintf
                        "var layer = %s ?? throw new %s(\"%s\", %s, \"%s\");"
                        g
                        s.WrongLayerExceptionType
                        name
                        s.VersionParam
                        g
                    :: [
                        for _, n in layer.Fields ->
                            let apiT =
                                apiTypes.TryFind n |> Option.map (csType s) |> Option.defaultValue "var"

                            sprintf "%s %s = layer.%s;" apiT n n
                    ]
                | None -> []

            unpack @ (results |> List.collect (function Ok ls -> ls | Error _ -> []))

    /// `public static PacketIdentity Identity => new(...)` — identity as a value, from the catalog.
    let private identityMember (s: RuntimeSurface) (e: Registry.CatalogEntry) : MemberDeclarationSyntax =
        let p = e.Spec

        let shortName =
            if p.ClassName.EndsWith "Packet" then
                p.ClassName.[.. p.ClassName.Length - 7]
            else
                p.ClassName

        ParseMemberDeclaration(
            sprintf
                "public static %s Identity => new(\"%s\", \"%s\", %s.%A, %s.%A, %d);"
                s.IdentityType
                e.Key
                shortName
                s.PhaseEnum
                p.State
                s.DirectionEnum
                p.Direction
                e.Ordinal
        )

    /// `[Packet("key", PacketPhase.X, PacketDirection.Y)]` — declarative identity for third-party
    /// Roslyn source generators; the runtime never reads it.
    let private packetAttr (s: RuntimeSurface) (e: Registry.CatalogEntry) : AttributeListSyntax =
        AttributeList(
            SingletonSeparatedList(
                Attribute(ParseName s.PacketAttributeName)
                    .WithArgumentList(
                        ParseAttributeArgumentList(
                            sprintf
                                "(\"%s\", %s.%A, %s.%A)"
                                e.Key
                                s.PhaseEnum
                                e.Spec.State
                                s.DirectionEnum
                                e.Spec.Direction
                        )
                    )
            )
        )

    /// `[PacketField(...)]` per api field: common fields carry their Present bounds, group fields
    /// one attribute per layer they live in. Third-party-generator channel; runtime never reads it.
    let private packetFieldAttrs
        (s: RuntimeSurface)
        (common: ApiField list)
        (layers: PacketLayer list)
        : AttributeListSyntax list
        =
        let attr (name: string) (typeName: string) (group: string option) (range: VersionRange) =
            let lo, hi = VersionRangeX.bounds range

            let named =
                [
                    match group with
                    | Some g -> yield sprintf "Group = \"%s\"" g
                    | None -> ()
                    match lo with
                    | Some v -> yield sprintf "From = %d" v
                    | None -> ()
                    match hi with
                    | Some v -> yield sprintf "To = %d" v
                    | None -> ()
                ]

            let args =
                String.concat ", " (sprintf "\"%s\", \"%s\"" name typeName :: named)

            AttributeList(
                SingletonSeparatedList(
                    Attribute(ParseName s.PacketFieldAttributeName)
                        .WithArgumentList(ParseAttributeArgumentList(sprintf "(%s)" args))
                )
            )

        [
            for f in common -> attr f.Name (csType s f.Type) None f.Present
            for l in layers do
                match l.GroupName with
                | Some g ->
                    for t, n in l.Fields do
                        yield attr n t (Some g) l.Layout.Range
                | None -> ()
        ]

    /// A packet renders in form A: a sealed record class — common fields positional, one nullable
    /// group per version layer, layer-guarded Read/Write — plus identity, Try/GetPacketId and the
    /// declarative attributes. Single-layout packets stay flat: all fields positional, no groups.
    /// A packet the manifest knows no ids for still implements the packet interface — its
    /// TryGetPacketId is always false.
    let private renderPacket (s: RuntimeSurface) (e: Registry.CatalogEntry) : string =
        let p = e.Spec
        let multi = List.length p.Layouts > 1

        let commonFields =
            if multi then p.ApiFields |> List.filter isCommon else p.ApiFields

        let layers =
            if multi then
                packetLayers s p
            else
                [
                    for l in p.Layouts ->
                        {
                            Layout = l
                            GroupName = None
                            Fields = []
                        }
                ]

        let commonPos = commonFields |> List.map (fun f -> csType s f.Type, f.Name)
        let apiTypes = p.ApiFields |> List.map (fun f -> f.Name, f.Type) |> Map.ofList

        let readBody =
            gateLine s p.ClassName
            :: versionedBody
                s
                p.ClassName
                false
                [
                    for l in layers -> l.Layout.Range, formAReadLines s p.ClassName commonFields l
                ]

        let writeBody =
            gateLine s p.ClassName
            :: versionedBody
                s
                p.ClassName
                true
                [
                    for l in layers -> l.Layout.Range, formAWriteLines s p.ClassName apiTypes l
                ]

        let shell =
            (packetRecordShell s.PacketInterface p.ClassName commonPos layers)
                .AddMembers(
                    readMethod s p.ClassName (parseBody readBody),
                    writeMethod s false (parseBody writeBody)
                )
                .AddMembers(identityMember s e :: packetIdMethods s p.Ids |> List.toArray)
                .AddAttributeLists(supportAttr s (p.Layouts |> List.map (fun l -> l.Range)))
                .AddAttributeLists(
                    packetAttr s e :: packetFieldAttrs s commonFields (if multi then layers else [])
                    |> List.toArray
                )

        renderUnit s (packetNamespace s p) p.ClassName (usingsFor s p.ApiFields) shell

    // ----- bitflags -----

    let private renderBitflags (s: RuntimeSurface) (spec: BitflagsSpec) : string =
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

        let shell =
            (recordStructShell s.ProtocolInterface name (apiFlags |> List.map (fun f -> "bool", pascal f)))
                .AddMembers(readMethod s name (parseBody readBody), writeMethod s true (parseBody writeBody))
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        renderUnit s s.Namespace name [ s.UsingAttributes; s.UsingSerialization ] shell

    // ----- whole-protocol aggregates: registry tables, dispatcher, handler base -----

    /// Validate + format a hand-assembled aggregate source file (several top-level types per
    /// file, so the single-decl `renderUnit` does not fit). Same hard gate: parse errors fail
    /// generation instead of writing the file.
    let private renderRawUnit (label: string) (text: string) : string =
        let cu = ParseCompilationUnit text

        let errors =
            cu.GetDiagnostics()
            |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
            |> Seq.toList

        if not errors.IsEmpty then
            failwithf
                "codegen: emitted invalid C# for %s:\n%s\n----\n%s"
                label
                (errors |> List.map string |> String.concat "\n")
                text

        cu.NormalizeWhitespace("    ", "\n", false).ToFullString() + "\n"

    let private allStates = [ Handshaking; Status; Login; Configuration; Play ]
    let private allDirs = [ Clientbound; Serverbound ]

    /// A packet whose read side is not fully generated for at least one layout. Excluded from
    /// dispatch and the handler base: its ordinal falls through to `Unknown`, so a stub `Read`
    /// throw can never take down the receive loop.
    let private hasReadStub (s: RuntimeSurface) (p: PacketSpec) =
        p.Layouts
        |> List.exists (fun l ->
            l.Entries
            |> List.exists (fun e ->
                match readEntryLines s e with
                | Error _ -> true
                | Ok _ -> false))

    let rec private wireNamedRefs (w: WireType) : string list =
        match w with
        | Named n -> [ n ]
        | Array(item, cnt) ->
            wireNamedRefs item
            @ (match cnt with
               | TypedCount t -> wireNamedRefs t
               | _ -> [])
        | Option inner -> wireNamedRefs inner
        | SentinelArray(item, _) -> wireNamedRefs item
        | Switch(_, cases) -> cases |> List.collect (fun c -> wireNamedRefs c.Type)
        | _ -> []

    let rec private entryNamedRefs (e: WireEntry) : string list =
        match e with
        | Read(_, w, _) -> wireNamedRefs w
        | Discard(_, w) -> wireNamedRefs w
        | IfNonZero(_, inner) -> inner |> List.collect entryNamedRefs
        | ReadOpt(_, w, _, _, _) -> wireNamedRefs w
        | ReadBlock(w, _, inner) -> wireNamedRefs w @ (inner |> List.collect entryNamedRefs)
        | ReadUnion(_, u, _) -> [ u ]
        | InlineUnion(_, arms) -> arms |> List.collect (fun a -> a.Entries |> List.collect entryNamedRefs)

    let rec private apiNamedRefs (t: ApiType) : string list =
        match t with
        | TArray inner
        | TOption inner -> apiNamedRefs inner
        | TNamed n -> [ n ]
        | TUnion n -> [ n ]
        | _ -> []

    /// Named types the delivered output can resolve: runtime-provided primitives plus generated
    /// named/bitflags types that are themselves stub-free and reference only resolvable types
    /// (fixpoint). Unions are not generated yet, so a union reference is unresolvable.
    let private resolvableTypes (s: RuntimeSurface) (protocol: ProtocolSpec) : Set<string> =
        let runtimeProvided = Set.ofList [ "Position" ]

        let stubFree (t: NamedTypeSpec) =
            t.Layouts
            |> List.forall (fun l ->
                l.Entries
                |> List.forall (fun e ->
                    match readEntryLines s e with
                    | Ok _ -> true
                    | Error _ -> false))

        let candidates =
            [
                for t in protocol.Types ->
                    let refs =
                        (t.ApiFields |> List.collect (fun f -> apiNamedRefs f.Type))
                        @ (t.Layouts |> List.collect (fun l -> l.Entries |> List.collect entryNamedRefs))

                    t.Name, refs, stubFree t
            ]

        let mutable known =
            Set.union runtimeProvided (protocol.Bitflags |> List.map (fun b -> b.Name) |> Set.ofList)

        let mutable changed = true

        while changed do
            changed <- false

            for name, refs, ok in candidates do
                if ok && not (known.Contains name) && refs |> List.forall known.Contains then
                    known <- known.Add name
                    changed <- true

        known

    /// A packet the dispatcher may reference: read side fully generated AND every named type it
    /// touches resolvable in the delivered output (mirrors the delivery exclusions by data,
    /// not by file name).
    let private isDispatchable (s: RuntimeSurface) (known: Set<string>) (p: PacketSpec) =
        let refs =
            (p.ApiFields |> List.collect (fun f -> apiNamedRefs f.Type))
            @ (p.Layouts |> List.collect (fun l -> l.Entries |> List.collect entryNamedRefs))

        not (hasReadStub s p) && refs |> List.forall known.Contains

    /// Packet type name relative to the root namespace (`Packets.Play.Clientbound.KeepAlivePacket`).
    let private relTypeName (p: PacketSpec) =
        sprintf "Packets.%A.%A.%s" p.State p.Direction p.ClassName

    let private shortName (p: PacketSpec) =
        if p.ClassName.EndsWith "Packet" then
            p.ClassName.[.. p.ClassName.Length - 7]
        else
            p.ClassName

    /// `Flow/PacketRegistry.g.cs`: descriptors + dense id->ordinal tables per pv-run.
    let private renderRegistryFile (s: RuntimeSurface) (entries: Registry.CatalogEntry list) : string =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    for dir in allDirs do
                        let slice = Registry.slice st dir entries
                        if not slice.IsEmpty then yield st, dir, slice
            ]

        // pv runs with an identical id -> ordinal layout, per slice
        let runsFor (slice: Registry.CatalogEntry list) =
            let ids = slice |> List.collect (fun e -> e.Spec.Ids)

            if ids.IsEmpty then
                []
            else
                let minPv = ids |> List.map (fun (lo, _, _) -> lo) |> List.min
                let maxPv = ids |> List.map (fun (_, hi, _) -> hi) |> List.max

                let mapAt pv =
                    [
                        for e in slice do
                            for lo, hi, id in e.Spec.Ids do
                                if pv >= lo && pv <= hi then
                                    yield id, e.Ordinal
                    ]
                    |> List.sortBy fst

                let mutable runs = []
                let mutable runLo = minPv
                let mutable current = mapAt minPv

                for pv in minPv + 1 .. maxPv do
                    let m = mapAt pv

                    if m <> current then
                        runs <- (runLo, pv - 1, current) :: runs
                        runLo <- pv
                        current <- m

                runs <- (runLo, maxPv, current) :: runs
                runs |> List.rev |> List.filter (fun (_, _, m) -> not (List.isEmpty m))

        line "using System;"
        line "using System.Diagnostics.CodeAnalysis;"
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line "public readonly record struct IdRange(int FromPv, int ToPv, int Id);"
        line ""
        line (sprintf "public sealed record PacketDescriptor(%s Identity, IdRange[] Ids);" s.IdentityType)
        line ""
        line "/// <summary>Generated packet registry: dense id->ordinal tables on the hot path,"
        line "/// descriptor catalogs on the cold one. Unknown ids are a normal stream condition:"
        line "/// every entry point is Try.</summary>"
        line "public static partial class PacketRegistry"
        line "{"

        for st, dir, slice in slices do
            line (sprintf "    private static readonly PacketDescriptor[] Catalog%A%A =" st dir)
            line "    ["

            for e in slice do
                let ids =
                    coalesceIds e.Spec.Ids
                    |> List.map (fun (lo, hi, id) -> sprintf "new(%d, %d, 0x%02X)" lo hi id)
                    |> String.concat ", "

                // identity inlined (same data the packet's own Identity is printed from): the
                // registry must not reference packet types — some are not deliverable yet.
                let identity =
                    sprintf
                        "new(\"%s\", \"%s\", %s.%A, %s.%A, %d)"
                        e.Key
                        (shortName e.Spec)
                        s.PhaseEnum
                        e.Spec.State
                        s.DirectionEnum
                        e.Spec.Direction
                        e.Ordinal

                line (sprintf "        new(%s, [%s])," identity ids)

            line "    ];"
            line ""

        for st, dir, slice in slices do
            for lo, hi, map in runsFor slice do
                let maxId = map |> List.map fst |> List.max
                let table = Array.create (maxId + 1) (-1)

                for id, ordinal in map do
                    table.[id] <- ordinal

                line (sprintf "    private static ReadOnlySpan<short> Table%A%A_%d_%d =>" st dir lo hi)
                line (sprintf "        [%s];" (table |> Seq.map string |> String.concat ", "))
                line ""

        line (sprintf "    private static ReadOnlySpan<short> Table(%s phase, %s dir, int pv)" s.PhaseEnum s.DirectionEnum)
        line "    {"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, slice in slices do
            let runs = runsFor slice

            if not runs.IsEmpty then
                line (sprintf "            case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)

                for lo, hi, _ in runs do
                    line (sprintf "                if (pv >= %d && pv <= %d) return Table%A%A_%d_%d;" lo hi st dir lo hi)

                line "                return default;"

        line "        }"
        line ""
        line "        return default;"
        line "    }"
        line ""
        line (
            sprintf
                "    public static bool TryGetOrdinal(int id, int %s, %s phase, %s dir, out ushort ordinal)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )
        line "    {"
        line (sprintf "        var table = Table(phase, dir, %s);" s.VersionParam)
        line "        if ((uint)id < (uint)table.Length && table[id] >= 0)"
        line "        {"
        line "            ordinal = (ushort)table[id];"
        line "            return true;"
        line "        }"
        line ""
        line "        ordinal = 0;"
        line "        return false;"
        line "    }"
        line ""
        line (
            sprintf
                "    public static bool TryResolve(int id, int %s, %s phase, %s dir, [NotNullWhen(true)] out PacketDescriptor? descriptor)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )
        line "    {"
        line (sprintf "        if (TryGetOrdinal(id, %s, phase, dir, out var ordinal))" s.VersionParam)
        line "        {"
        line "            descriptor = Catalog(phase, dir)[ordinal];"
        line "            return true;"
        line "        }"
        line ""
        line "        descriptor = null;"
        line "        return false;"
        line "    }"
        line ""
        line (sprintf "    public static ReadOnlySpan<PacketDescriptor> Catalog(%s phase, %s dir)" s.PhaseEnum s.DirectionEnum)
        line "    {"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (sprintf "            case (%s.%A, %s.%A): return Catalog%A%A;" s.PhaseEnum st s.DirectionEnum dir st dir)

        line "        }"
        line ""
        line "        return default;"
        line "    }"
        line "}"
        sb.ToString()

    /// `Flow/PacketFlow.g.cs`: one lookup + one ordinal jump table + one constrained call per
    /// packet. Dispatch is deliberately synchronous: the decode must finish before the next
    /// transport read (the `InputPacket.Data` window); anything async happens in the facade after.
    let private renderFlowFile
        (s: RuntimeSurface)
        (dispatchable: PacketSpec -> bool)
        (entries: Registry.CatalogEntry list)
        : string
        =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    for dir in allDirs do
                        let slice =
                            Registry.slice st dir entries
                            |> List.filter (fun e -> dispatchable e.Spec)

                        if not slice.IsEmpty then yield st, dir, slice
            ]

        line "using McProtoNet.Net;"
        line (sprintf "using %s;" s.UsingSerialization)
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line (sprintf "public delegate void TrailingBytesHook(int packetId, int %s, long remainingBytes);" s.VersionParam)
        line ""
        line "/// <summary>Generated dispatcher. Packets whose codegen is still stubbed are not"
        line "/// dispatched — they fall through to <c>Unknown</c> instead of throwing inside the"
        line "/// receive loop. Trailing bytes raise a hook, not an exception: the packet already"
        line "/// reached the visitor, but the spec is suspect.</summary>"
        line "public static partial class PacketFlow"
        line "{"
        line "    public static event TrailingBytesHook? OnTrailingBytes;"
        line ""
        line (
            sprintf
                "    public static void Dispatch<TVisitor>(in InputPacket raw, int %s, %s phase, %s dir, ref TVisitor visitor)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )
        line "        where TVisitor : IPacketVisitor"
        line "    {"
        line (sprintf "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, dir, out var ordinal))" s.VersionParam)
        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return;"
        line "        }"
        line ""
        line (sprintf "        var %s = new %s(raw.Data);" s.ReaderParam s.ReaderType)
        line "        bool handled;"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (sprintf "            case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)
            line (
                sprintf
                    "                handled = Dispatch%A%A(ordinal, ref %s, %s, ref visitor);"
                    st
                    dir
                    s.ReaderParam
                    s.VersionParam
            )
            line "                break;"

        line "            default:"
        line "                handled = false;"
        line "                break;"
        line "        }"
        line ""
        line "        if (!handled)"
        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return;"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)
        line (sprintf "            OnTrailingBytes?.Invoke(raw.Id, %s, %s.RemainingCount);" s.VersionParam s.ReaderParam)
        line "    }"

        for st, dir, slice in slices do
            line ""
            line (
                sprintf
                    "    private static bool Dispatch%A%A<TVisitor>(ushort ordinal, ref %s %s, int %s, ref TVisitor visitor)"
                    st
                    dir
                    s.ReaderType
                    s.ReaderParam
                    s.VersionParam
            )
            line "        where TVisitor : IPacketVisitor"
            line "    {"
            line "        switch (ordinal)"
            line "        {"

            for e in slice do
                line (sprintf "            case %d:" e.Ordinal)
                line (
                    sprintf
                        "                visitor.Visit(%s.Read(ref %s, %s));"
                        (relTypeName e.Spec)
                        s.ReaderParam
                        s.VersionParam
                )
                line "                return true;"

            line "            default:"
            line "                return false;"
            line "        }"
            line "    }"

        line "}"
        sb.ToString()

    /// Handler method name: bare `On{Short}` while unique across clientbound; on a collision
    /// Play keeps the bare name and other phases get a phase prefix.
    let private handlerNames (entries: Registry.CatalogEntry list) : Map<string * string, string> =
        let counts =
            entries
            |> List.map (fun e -> shortName e.Spec)
            |> List.countBy id
            |> Map.ofList

        entries
        |> List.map (fun e ->
            let short = shortName e.Spec

            let name =
                if counts.[short] = 1 || e.Spec.State = Play then
                    sprintf "On%s" short
                else
                    sprintf "On%A%s" e.Spec.State short

            (sprintf "%A" e.Spec.State, e.Spec.ClassName), name)
        |> Map.ofList

    /// `Flow/ClientboundHandler.g.cs`: one base for every clientbound phase, phase slot led by
    /// the consumer, ValueTask handlers awaited by the facade after the synchronous dispatch.
    let private renderHandlerFile
        (s: RuntimeSurface)
        (dispatchable: PacketSpec -> bool)
        (entries: Registry.CatalogEntry list)
        : string
        =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let clientbound =
            [
                for st in allStates do
                    let slice =
                        Registry.slice st Clientbound entries
                        |> List.filter (fun e -> dispatchable e.Spec)

                    if not slice.IsEmpty then yield st, slice
            ]

        let names = handlerNames (clientbound |> List.collect snd)

        line "using System.Threading.Tasks;"
        line "using McProtoNet.Net;"
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line "/// <summary>Generated handler base over every clientbound phase. The truth about"
        line "/// the current phase is the consumer's: set <see cref=\"Phase\"/> as the connection"
        line "/// advances. <c>HandleAsync</c> decodes synchronously (the raw data window must not"
        line "/// cross an await) and awaits the handler's result after. <c>OnUnknown</c> must not"
        line "/// hold on to <c>raw</c> beyond the call.</summary>"
        line "public abstract partial class ClientboundHandler : IPacketVisitor"
        line "{"
        line "    private ValueTask _pending;"
        line ""
        line (sprintf "    public %s Phase { get; protected set; } = %s.Login;" s.PhaseEnum s.PhaseEnum)
        line ""
        line (sprintf "    protected static %s Direction => %s.Clientbound;" s.DirectionEnum s.DirectionEnum)
        line ""
        line (sprintf "    public ValueTask HandleAsync(in InputPacket raw, int %s)" s.VersionParam)
        line "    {"
        line "        _pending = default;"
        line "        var self = this;"
        line (sprintf "        PacketFlow.Dispatch(in raw, %s, Phase, %s.Clientbound, ref self);" s.VersionParam s.DirectionEnum)
        line "        return _pending;"
        line "    }"
        line ""
        line "    void IPacketVisitor.Visit<T>(T packet)"
        line "    {"
        line "        var identity = T.Identity;"
        line "        switch (identity.Phase)"
        line "        {"

        for st, slice in clientbound do
            line (sprintf "            case %s.%A:" s.PhaseEnum st)
            line "                switch (identity.Ordinal)"
            line "                {"

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line (sprintf "                    case %d:" e.Ordinal)
                line (
                    sprintf
                        "                        _pending = %s((%s)(object)packet);"
                        handler
                        (relTypeName e.Spec)
                )
                line "                        return;"

            line "                }"
            line ""
            line "                return;"

        line "        }"
        line "    }"
        line ""
        line "    void IPacketVisitor.Unknown(in InputPacket raw) => _pending = OnUnknown(in raw);"
        line ""
        line "    protected virtual ValueTask OnUnknown(in InputPacket raw) => default;"

        for st, slice in clientbound do
            line ""
            line (sprintf "    // --- %A ---" st)

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line ""
                line (
                    sprintf
                        "    protected virtual ValueTask %s(%s packet) => default;"
                        handler
                        (relTypeName e.Spec)
                )

        line "}"
        sb.ToString()

    /// Aggregate outputs under `Flow/`. Excluded from the sandbox (they need the real transport
    /// types); their real test is the McProtoNet build after delivery. The registry covers the
    /// whole catalog (identities inlined); dispatcher and handler reference only packets that
    /// are stub-free and whose named types are resolvable in the delivered output.
    let private renderProtocolExtras (s: RuntimeSurface) (protocol: ProtocolSpec) : GeneratedFile list =
        let entries = Registry.catalog protocol.Packets
        let known = resolvableTypes s protocol
        let dispatchable (p: PacketSpec) = isDispatchable s known p

        [
            {
                RelativePath = "Flow/PacketRegistry.g.cs"
                Contents = renderRawUnit "PacketRegistry" (renderRegistryFile s entries)
            }
            {
                RelativePath = "Flow/PacketFlow.g.cs"
                Contents = renderRawUnit "PacketFlow" (renderFlowFile s dispatchable entries)
            }
            {
                RelativePath = "Flow/ClientboundHandler.g.cs"
                Contents = renderRawUnit "ClientboundHandler" (renderHandlerFile s dispatchable entries)
            }
        ]

    // ----- target -----

    /// A C# code-generation target bound to a specific runtime surface.
    let targetFor (surface: RuntimeSurface) : ILanguageTarget =
        { new ILanguageTarget with
            member _.Id = "csharp"
            member _.Extension = ".cs"

            member _.RenderType spec =
                renderType surface surface.Namespace surface.ProtocolInterface spec [] []

            member _.RenderBitflags spec = renderBitflags surface spec
            member _.RenderPacket entry = renderPacket surface entry
            member _.RenderProtocol entries = renderProtocolExtras surface entries
        }

    /// The default C# target: the McProtoNet runtime surface.
    let target: ILanguageTarget = targetFor mcProtoNet
