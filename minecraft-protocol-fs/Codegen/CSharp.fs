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
        set
            [
                "abstract"
                "as"
                "base"
                "bool"
                "break"
                "byte"
                "case"
                "catch"
                "char"
                "checked"
                "class"
                "const"
                "continue"
                "decimal"
                "default"
                "delegate"
                "do"
                "double"
                "else"
                "enum"
                "event"
                "explicit"
                "extern"
                "false"
                "finally"
                "fixed"
                "float"
                "for"
                "foreach"
                "goto"
                "if"
                "implicit"
                "in"
                "int"
                "interface"
                "internal"
                "is"
                "lock"
                "long"
                "namespace"
                "new"
                "null"
                "object"
                "operator"
                "out"
                "override"
                "params"
                "private"
                "protected"
                "public"
                "readonly"
                "ref"
                "return"
                "sbyte"
                "sealed"
                "short"
                "sizeof"
                "stackalloc"
                "static"
                "string"
                "struct"
                "switch"
                "this"
                "throw"
                "true"
                "try"
                "typeof"
                "uint"
                "ulong"
                "unchecked"
                "unsafe"
                "ushort"
                "using"
                "virtual"
                "void"
                "volatile"
                "while"
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
        | TUuid
        | TEnum _ -> true
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

    /// A discriminator the layer has no arm for: a stream condition, not a codegen gap.
    let private throwNoCaseLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s has no case for discriminator {%s} at protocol version {%s}.\");"
            typeName
            s.DiscriminatorParam
            s.VersionParam

    /// A case whose layer does not cover the version being written: the model holds a shape this
    /// version cannot carry, so the write must fail rather than invent one.
    let private throwNoCaseLayerLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s case {GetType().Name} has no wire layout for protocol version {%s}.\");"
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
        | EnumRef(n, _) -> Some n
        | RegistryHolder inner -> holderCsType s inner
        | FixedBytes _ -> Some "byte[]"
        | _ -> s.Primitives.TryFind w |> Option.map (fun p -> p.CsType)

    /// C# type of a whole wire field, wrappers included — what a union case declares as a
    /// positional parameter. `None` means the shape has no renderer yet.
    let rec private wireCsType (s: RuntimeSurface) (w: WireType) : string option =
        match w with
        | Option inner -> wireCsType s inner |> Option.map (fun t -> t + "?")
        | Array(item, _) -> wireCsType s item |> Option.map (fun t -> t + "[]")
        | _ -> itemCsType s w

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

    /// `new T[count]` — the count belongs in the *first* pair of brackets, so an item type that is
    /// itself an array (`byte[]`) becomes `new byte[count][]`, never `new byte[][count]`.
    let private newArrayExpr (itemType: string) (count: string) =
        match itemType.IndexOf '[' with
        | -1 -> sprintf "new %s[%s]" itemType count
        | i -> sprintf "new %s[%s]%s" (itemType.Substring(0, i)) count (itemType.Substring i)

    /// `T` -> `T?`, idempotent: a conditional group declares its locals nullable so the guard can
    /// leave them unset, and the api field it feeds is optional for exactly the same reason.
    let private nullableOf (t: string) = if t.EndsWith "?" then t else t + "?"

    /// The api names a read entry binds, paired with the C# type of the local it declares. A
    /// conditional group hoists these above its guard; a block passes them to the ctor.
    let rec private entryBindings (s: RuntimeSurface) (entry: WireEntry) : (string * string) list =
        let one api w =
            wireCsType s w |> Option.map (fun t -> [ api, t ]) |> Option.defaultValue []

        match entry with
        | Read(_, w, api) -> one api w
        | ReadOpt(_, w, api, _, _) -> one api w |> List.map (fun (a, t) -> a, nullableOf t)
        | ReadBlock(w, api, _) -> one api w
        | IfNonZero(_, inner) ->
            inner
            |> List.collect (entryBindings s)
            |> List.map (fun (a, t) -> a, nullableOf t)
        | Discard _
        | ReadUnion _
        | InlineUnion _ -> []

    /// Wire *and* api spellings of every field a layout reads, both pointing at the api name and
    /// the wire type it travelled as. `ifNonZero`/`readOpt` name their discriminator with either
    /// spelling (`ifNonZero "columns"` against `read "columns" U8 "Columns"`), so both must
    /// resolve; the wire type is what lets the write side guard on the *narrowed* value the read
    /// side will see.
    let private fieldNames (entries: WireEntry list) : Map<string, string * WireType> =
        entries
        |> List.collect (function
            | Read(wire, wt, api) -> [ wire, (api, wt); api, (api, wt) ]
            | _ -> [])
        |> Map.ofList

    /// An entry a conditional group cannot hoist: it would read inside the guard, bind nothing,
    /// and leave the api field `default!` while the bytes were consumed. A stub is the honest
    /// answer — `Discard` is the one entry that legitimately binds nothing.
    let private unbindableInGroup (s: RuntimeSurface) (entries: WireEntry list) =
        entries
        |> List.tryFind (function
            | Discard _ -> false
            | e -> entryBindings s e |> List.isEmpty)

    /// The natural api types of a block's own entries — a block's fields belong to the nested
    /// type, not to the packet, so the packet's api map cannot answer narrowing casts for them.
    let private naturalApiTypes (entries: WireEntry list) : Map<string, ApiType> =
        entries
        |> List.choose (function
            | Read(_, w, api) ->
                try
                    Some(api, apiOf w)
                with _ ->
                    None
            | _ -> None)
        |> Map.ofList

    /// One wire entry -> read statement lines. `bound` maps every name the entries before this one
    /// in the same layout already read (wire and api spelling) to its api name — the only names an
    /// entry may address. `nameOf` turns an api name into the local holding it, which is how a
    /// group renders its body into fresh locals before assigning the hoisted ones.
    let rec private readEntryLines
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (bound: Map<string, string>)
        (entry: WireEntry)
        : Result<string list, string>
        =
        match entry with
        | Read(_, Option inner, api) ->
            let ln = nameOf api

            match readExpr s inner, itemCsType s inner with
            | Ok call, Some t ->
                Ok
                    [
                        sprintf "%s? %s = null;" t ln
                        sprintf "if (%s.ReadBoolean()) %s = %s;" s.ReaderParam ln call
                    ]
            | _ -> Error(sprintf "read '%s' (Option %A)" api inner)
        | Read(_, Array(item, cnt), api) ->
            let ln = nameOf api

            match readExpr s item, itemCsType s item, countRead s cnt ln with
            | Ok call, Some t, Some(setup, cntExpr) ->
                Ok(
                    setup
                    @ [
                        sprintf "var %s = %s;" ln (newArrayExpr t cntExpr)
                        sprintf "for (int i = 0; i < %s.Length; i++) %s[i] = %s;" ln ln call
                    ]
                )
            | _ -> Error(sprintf "read '%s' (Array %A)" api item)
        | Read(_, wt, api) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "var %s = %s;" (nameOf api) call ]
            | Error e -> Error(sprintf "read '%s' (%s)" api e)
        | Discard(wire, Option inner) ->
            // wrappers nest, so the inner shape is discarded by the same renderer one level down
            match readEntryLines s nameOf bound (Discard(wire, inner)) with
            | Ok ls -> Ok([ sprintf "if (%s.ReadBoolean())" s.ReaderParam; "{" ] @ ls @ [ "}" ])
            | Error e -> Error(sprintf "discard '%s' (Option: %s)" wire e)
        | Discard(wire, Array(item, cnt)) ->
            let ln = "skip" + pascal (camel wire)

            match countRead s cnt ln, readEntryLines s nameOf bound (Discard(wire + "Item", item)) with
            | Some(setup, cntExpr), Ok ls ->
                Ok(
                    setup
                    @ [ sprintf "for (int %sI = 0; %sI < %s; %sI++)" ln ln cntExpr ln; "{" ]
                    @ ls
                    @ [ "}" ]
                )
            | _, Error e -> Error(sprintf "discard '%s' (Array: %s)" wire e)
            | None, _ -> Error(sprintf "discard '%s' (Array count %A)" wire cnt)
        | Discard(wire, wt) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | ReadUnion(disc, _, api) when (bound.TryFind disc).IsNone ->
            // Nothing read '%s' yet, so the emitted call would name a local that does not exist:
            // valid-looking C# that cannot compile. A stub is the honest answer.
            Error(sprintf "read union '%s' (discriminator '%s' is not read by an earlier entry)" api disc)
        | ReadUnion(disc, unionName, api) ->
            // the discriminator is a plain wire field an earlier entry already read into a local;
            // the cast normalizes whatever integer it was to the union's `int discriminator`
            Ok
                [
                    sprintf
                        "var %s = %s.%s(ref %s, %s, (int)%s);"
                        (nameOf api)
                        unionName
                        s.ReadMethodName
                        s.ReaderParam
                        s.VersionParam
                        (nameOf bound.[disc])
                ]
        | IfNonZero(field, entries) ->
            match bound.TryFind field with
            | None -> Error(sprintf "conditional group (field '%s' is not read by an earlier entry)" field)
            | Some api -> readGroupLines s nameOf bound (sprintf "%s != 0" (nameOf api)) entries
        | ReadOpt(wire, wt, api, disc, keys) ->
            match bound.TryFind disc with
            | None -> Error(sprintf "read optional '%s' (discriminator '%s' is not read by an earlier entry)" api disc)
            | Some discApi ->
                let cond =
                    keys
                    |> List.map (fun k -> sprintf "%s == %d" (nameOf discApi) k)
                    |> String.concat " || "

                readGroupLines s nameOf bound cond [ Read(wire, wt, api) ]
        | ReadBlock(Named typeName, api, entries) ->
            let ln = nameOf api
            let innerName (a: string) = ln + pascal a
            let results = readEntriesFold s innerName bound entries

            match firstReadError results, unbindableInGroup s entries with
            | Some e, _ -> Error(sprintf "read block '%s' (%s)" api e)
            | _, Some e -> Error(sprintf "read block '%s' holds an entry it cannot pass to the ctor: %A" api e)
            | None, None ->
                // positional: the ctor parameter *names* differ by shell (positional record struct
                // keeps the api name, class camel-cases it), so a block's entries must be listed
                // in the target type's api order — which is how a container is modelled anyway.
                let args =
                    entries
                    |> List.collect (entryBindings s)
                    |> List.map (fun (a, _) -> innerName a)
                    |> String.concat ", "

                Ok(readLinesOf results @ [ sprintf "var %s = new %s(%s);" ln typeName args ])
        | other -> Error(sprintf "%A" other)

    /// A conditional group: every local the body binds is declared (nullable) above the guard, the
    /// body reads into its own locals inside it, and the hoisted ones are assigned at the end. The
    /// inner names keep the C# legal — a nested scope may not shadow an outer local.
    and private readGroupLines
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (bound: Map<string, string>)
        (cond: string)
        (entries: WireEntry list)
        : Result<string list, string>
        =
        let innerName (a: string) = nameOf a + "Value"
        let results = readEntriesFold s innerName bound entries

        match firstReadError results, unbindableInGroup s entries with
        | Some e, _ -> Error e
        | _, Some e -> Error(sprintf "conditional group holds an entry it cannot hoist: %A" e)
        | None, None ->
            let bindings = entries |> List.collect (entryBindings s)

            Ok(
                [ for a, t in bindings -> sprintf "%s %s = default;" (nullableOf t) (nameOf a) ]
                @ [ sprintf "if (%s)" cond; "{" ]
                @ readLinesOf results
                @ [ for a, _ in bindings -> sprintf "%s = %s;" (nameOf a) (innerName a) ]
                @ [ "}" ]
            )

    /// Read entries in layout order, pairing each with its rendered lines. The fold is what makes
    /// "an earlier entry bound this" checkable: an entry only ever sees the names before it.
    and private readEntriesFold
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (start: Map<string, string>)
        (entries: WireEntry list)
        : (WireEntry * Result<string list, string>) list
        =
        entries
        |> List.mapFold
            (fun bound e ->
                let rendered = readEntryLines s nameOf bound e

                let bound =
                    match e with
                    | Read(wire, _, api) -> bound |> Map.add wire api |> Map.add api api
                    | _ -> entryBindings s e |> List.fold (fun m (a, _) -> Map.add a a m) bound

                (e, rendered), bound)
            start
        |> fst

    and private firstReadError (results: (WireEntry * Result<string list, string>) list) : string option =
        results
        |> List.tryPick (function
            | _, Error e -> Some e
            | _ -> None)

    and private readLinesOf (results: (WireEntry * Result<string list, string>) list) : string list =
        results
        |> List.collect (function
            | _, Ok ls -> ls
            | _, Error _ -> [])

    let private readEntriesLines (s: RuntimeSurface) (entries: WireEntry list) =
        readEntriesFold s (localName s) Map.empty entries

    /// Locals a rendered layout leaves behind, by local name — the set a ctor call may draw on.
    let private boundLocals (s: RuntimeSurface) (results: (WireEntry * Result<string list, string>) list) =
        results
        |> List.collect (function
            | ReadUnion(_, _, api), Ok _ -> [ localName s api ]
            | e, Ok _ -> entryBindings s e |> List.map (fst >> localName s)
            | _, Error _ -> [])
        |> Set.ofList

    let private hasReadError (results: (WireEntry * Result<string list, string>) list) =
        results
        |> List.exists (function
            | _, Error _ -> true
            | _ -> false)

    /// Wire-only discriminator -> the api field of the union that consumes it. The write side has
    /// no such wire field in the model, so the union's own `Discriminator(pv)` is its only source.
    let private discriminatorUnions (entries: WireEntry list) : Map<string, string> =
        entries
        |> List.choose (function
            | ReadUnion(disc, _, api) -> Some(disc, api)
            | _ -> None)
        |> Map.ofList

    /// What a write body needs beyond the entry itself. `Access` is how a value is spelled in C#:
    /// the api name at the top level, `block.Field` inside a block — the one place a nested
    /// container differs from the packet's own fields.
    type private WriteCtx =
        {
            ApiTypes: Map<string, ApiType>
            DiscUnions: Map<string, string>
            Fields: Map<string, string * WireType>
            Access: string -> string
        }

    let private writeCtxOf (apiTypes: Map<string, ApiType>) (entries: WireEntry list) : WriteCtx =
        {
            ApiTypes = apiTypes
            DiscUnions = discriminatorUnions entries
            Fields = fieldNames entries
            Access = id
        }

    /// The value a conditional guard tests, spelled exactly as the wire will carry it. The read
    /// side guards on the byte it read back, so a wider api value must be narrowed here too:
    /// `Flag = 256` over a `U8` wire writes `0`, and an un-narrowed `Flag != 0` would then write a
    /// block the reader never looks for — a silent stream desync.
    let private discValue (s: RuntimeSurface) (ctx: WriteCtx) (api: string) (wt: WireType) : string =
        let acc = ctx.Access api

        let apiT =
            match ctx.ApiTypes.TryFind api with
            | Some(TOption t) -> Some t
            | other -> other

        match s.Primitives.TryFind wt, apiT with
        | Some p, Some t when csType s t <> p.CsType -> sprintf "(%s)%s" p.CsType acc
        | _ -> acc

    /// One wire entry -> write statement lines. `ctx.ApiTypes` drives narrowing casts;
    /// `ctx.DiscUnions` pairs a wire-only discriminator with the union field that derives its value.
    let rec private writeEntryLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (entry: WireEntry)
        : Result<string list, string>
        =
        let apiTypes = ctx.ApiTypes
        let discUnions = ctx.DiscUnions

        match entry with
        | Read(_, wt, api) when api.StartsWith "_" ->
            // wire-only field: the model carries no such field, so the value must be derived. Only
            // a union consumer knows how — anything else stays a gap.
            match discUnions.TryFind api with
            | None -> Error(sprintf "write wire-only '%s' (derive from model)" api)
            | Some unionApi ->
                let disc = sprintf "%s.%s(%s)" unionApi s.DiscriminatorMethodName s.VersionParam

                // `Discriminator` hands back the api-level integer; a narrower wire primitive must
                // report a key it cannot carry instead of wrapping it (same rule as `countRead`).
                let value =
                    match s.Primitives.TryFind wt with
                    | Some p when p.CsType <> csType s TInt -> sprintf "checked((%s)%s)" p.CsType disc
                    | _ -> disc

                match writeExpr s wt None value with
                | Ok call -> Ok [ sprintf "%s;" call ]
                | Error e -> Error(sprintf "write discriminator '%s' (%s)" api e)
        | Read(_, Option inner, api) ->
            let v = camel api + "Value"
            let acc = ctx.Access api

            match writeExpr s inner None v with
            | Ok call ->
                Ok
                    [
                        sprintf "%s.WriteBoolean(%s is not null);" s.WriterParam acc
                        sprintf "if (%s is { } %s) %s;" acc v call
                    ]
            | Error e -> Error(sprintf "write '%s' (Option: %s)" api e)
        | Read(_, Array(item, cnt), api) ->
            let iv = camel api + "Item"

            // same rule as the scalar branch below: an option-typed api field written as a
            // required wire value must be present, or the count and the items disagree
            let acc =
                match apiTypes.TryFind api with
                | Some(TOption _) ->
                    sprintf
                        "(%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\"))"
                        (ctx.Access api)
                        api
                | _ -> ctx.Access api

            match writeExpr s item None iv, countWrite s cnt acc with
            | Ok call, Some cw -> Ok(cw @ [ sprintf "foreach (var %s in %s) %s;" iv acc call ])
            | _ -> Error(sprintf "write '%s' (Array %A)" api item)
        | Read(_, wt, api) ->
            // an option-typed api field written as a required wire value must be present
            let apiT = apiTypes.TryFind api

            let requiredT =
                match apiT with
                | Some(TOption t) -> Some t
                | other -> other

            let acc = ctx.Access api

            let value =
                match apiT with
                | Some(TOption _) ->
                    sprintf
                        "(%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\"))"
                        acc
                        api
                | _ -> acc
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
                | FixedBytes n -> sprintf "new byte[%d]" n
                | _ -> "default"

            match writeExpr s wt None value with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | ReadUnion(_, _, api) ->
            Ok
                [
                    sprintf "%s.%s(%s, %s);" (ctx.Access api) s.WriteMethodName s.WriterParam s.VersionParam
                ]
        | IfNonZero(field, entries) ->
            match ctx.Fields.TryFind field with
            | None -> Error(sprintf "write conditional group (field '%s' is not a wire field of this layout)" field)
            | Some(api, wt) -> writeGroupLines s ctx (sprintf "%s != 0" (discValue s ctx api wt)) entries
        | ReadOpt(wire, wt, api, disc, keys) ->
            match ctx.Fields.TryFind disc with
            | None ->
                Error(sprintf "write optional '%s' (discriminator '%s' is not a wire field of this layout)" api disc)
            | Some(discApi, discWt) ->
                let v = discValue s ctx discApi discWt

                let cond = keys |> List.map (sprintf "%s == %d" v) |> String.concat " || "

                match writeGroupLines s ctx cond [ Read(wire, wt, api) ] with
                | Error e -> Error e
                | Ok lines ->
                    // the mirror of the `?? throw` inside the guard: a value the discriminator
                    // says is absent has nowhere to go, and dropping it silently loses data
                    let elseThrow =
                        match ctx.ApiTypes.TryFind api with
                        | Some(TOption _) ->
                            [
                                sprintf "else if (%s is not null)" (ctx.Access api)
                                "{"
                                sprintf
                                    "throw new System.InvalidOperationException(\"%s is set, but '%s' does not select it at this protocol version.\");"
                                    api
                                    disc
                                "}"
                            ]
                        | _ -> []

                    Ok(lines @ elseThrow)
        | ReadBlock(Named typeName, api, entries) ->
            let ln = localName s api

            let inner =
                {
                    ApiTypes = naturalApiTypes entries
                    DiscUnions = Map.empty
                    Fields = fieldNames entries
                    Access = fun a -> ln + "." + a
                }

            let results = entries |> List.map (writeEntryLines s inner)

            match
                results
                |> List.tryPick (function
                    | Error e -> Some e
                    | _ -> None)
            with
            | Some e -> Error(sprintf "write block '%s' (%s)" api e)
            | None ->
                let source =
                    match ctx.ApiTypes.TryFind api with
                    | Some(TOption _) ->
                        sprintf
                            "%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\")"
                            (ctx.Access api)
                            api
                    | _ -> ctx.Access api

                Ok(
                    [ sprintf "%s %s = %s;" typeName ln source ]
                    @ (results
                       |> List.collect (function
                           | Ok ls -> ls
                           | Error _ -> []))
                )
        | other -> Error(sprintf "%A" other)

    /// A conditional group on the write side: the same guard the read side applies, over the api
    /// value the group's discriminator field carries.
    and private writeGroupLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (cond: string)
        (entries: WireEntry list)
        : Result<string list, string>
        =
        let results = entries |> List.map (writeEntryLines s ctx)

        match
            results
            |> List.tryPick (function
                | Error e -> Some e
                | _ -> None)
        with
        | Some e -> Error e
        | None ->
            Ok(
                [ sprintf "if (%s)" cond; "{" ]
                @ (results
                   |> List.collect (function
                       | Ok ls -> ls
                       | Error _ -> []))
                @ [ "}" ]
            )

    // ----- per-layout bodies + version branching -----

    let private layoutReadLines
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

    let private layoutWriteLines
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
                    Parameter(Identifier "id").WithType(ParseTypeName "int").AddModifiers(Token SyntaxKind.OutKeyword)
                )
                .WithBody(parseBody tryBody)

        let getBody =
            [
                sprintf "if (TryGetPacketId(%s, out var id)) return id;" s.VersionParam
                sprintf "throw new System.NotSupportedException($\"No packet id for protocol {%s}.\");" s.VersionParam
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

    /// Mechanical group name from a layout range: `V759`, `V761_763`, `V764_Last`. Shared with the
    /// union backend, which labels its per-layer cases the same way — one naming scheme, not two.
    let private layerName (allRanges: VersionRange list) (r: VersionRange) : string =
        match VersionRangeX.bounds r with
        | Some a, Some b when a = b -> sprintf "V%d" a
        | Some a, Some b -> sprintf "V%d_%d" a b
        | Some a, None -> sprintf "V%d_Last" a
        | None, Some b ->
            match VersionRangeX.span allRanges |> fst with
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

        // a field bound anywhere under a conditional group is optional in this layer too: the
        // guard may leave it unset, and the read side hoists exactly such a local as nullable
        let rec optionalIn (entries: WireEntry list) =
            entries
            |> List.exists (function
                | Read(_, Option _, api) -> api = f.Name
                | ReadOpt(_, _, api, _, _) -> api = f.Name
                | IfNonZero(_, inner) -> boundApis inner |> List.contains f.Name || optionalIn inner
                | _ -> false)

        let optionalHere = optionalIn l.Entries

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
                    GroupName =
                        (if fields.IsEmpty then
                             None
                         else
                             Some(layerName (p.Layouts |> List.map (fun l -> l.Range)) l.Range))
                    Fields = fields
                }
        ]

    /// `public sealed partial record Name(TCommon A, V759Layer? V759 = null, ...) : IPacket<Name>, IPacket`
    /// with one nested `readonly record struct {G}Layer(...)` per group. The second, non-generic
    /// interface (`baseIface`) is what a decoded packet answers to once its static type is gone;
    /// nested named types never get it — this shell is packets only.
    let private packetRecordShell
        (iface: string option)
        (baseIface: string option)
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
                                (l.Fields |> List.map (fun (t, n) -> sprintf "%s %s" t n) |> String.concat ", ")
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
            .AddBaseListTypes(
                [|
                    for i in Option.toList baseIface -> SimpleBaseType(ParseTypeName i) :> BaseTypeSyntax
                |]
            )
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
        let results = readEntriesLines s layer.Layout.Entries

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

            lines
            @ [
                sprintf "return new %s(%s);" name (String.concat ", " (commonArgs @ groupArg))
            ]

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
        let results =
            layer.Layout.Entries
            |> List.map (writeEntryLines s (writeCtxOf apiTypes layer.Layout.Entries))

        let errors =
            results
            |> List.choose (function
                | Error e -> Some e
                | _ -> None)

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
                            let apiT = apiTypes.TryFind n |> Option.map (csType s) |> Option.defaultValue "var"

                            sprintf "%s %s = layer.%s;" apiT n n
                    ]
                | None -> []

            unpack
            @ (results
               |> List.collect (function
                   | Ok ls -> ls
                   | Error _ -> []))

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

    /// `PacketIdentity IPacket.Identity => Identity;` — the same value the type answers statically,
    /// reachable through a plain reference. Explicit on purpose: an explicit implementation is not a
    /// named member of the class, so the instance property and the static one coexist, and the bare
    /// `Identity` inside the body binds to the static one (no recursion).
    let private identityBaseMember (s: RuntimeSurface) (baseIface: string) : MemberDeclarationSyntax =
        ParseMemberDeclaration(sprintf "%s %s.Identity => Identity;" s.IdentityType baseIface)

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

            let args = String.concat ", " (sprintf "\"%s\", \"%s\"" name typeName :: named)

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
            if multi then
                p.ApiFields |> List.filter isCommon
            else
                p.ApiFields

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
                [ for l in layers -> l.Layout.Range, formAWriteLines s p.ClassName apiTypes l ]

        let identityMembers =
            identityMember s e
            :: [ for i in Option.toList s.PacketBaseInterface -> identityBaseMember s i ]

        let shell =
            (packetRecordShell s.PacketInterface s.PacketBaseInterface p.ClassName commonPos layers)
                .AddMembers(readMethod s p.ClassName (parseBody readBody), writeMethod s false (parseBody writeBody))
                .AddMembers(identityMembers @ packetIdMethods s p.Ids |> List.toArray)
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

    let private renderEnum (s: RuntimeSurface) (spec: EnumSpec) : string =
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

        let shell =
            (recordStructShell s.ProtocolInterface name [ "int", "Value" ])
                .AddMembers(List.toArray (constants @ conversions))
                .AddMembers(readMethod s name (parseBody readBody), writeMethod s true (parseBody writeBody))
                .AddMembers(List.toArray toStringMembers)
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))

        // CA2225 wants a named twin for every conversion operator (`ToInt32`, `FromDifficulty`).
        // The named form is already the type's whole surface — `Value` reads the int out and the
        // primary constructor puts one back — so the analyzer would only buy duplicate spellings.
        "#pragma warning disable CA2225\n\n"
        + renderUnit s s.Namespace name [ s.UsingAttributes; s.UsingSerialization ] shell

    // ----- unions -----

    /// One case of a rendered union: the nested record's name and its positional parameters.
    type private UnionCase =
        {
            CaseName: string
            Params: (string * string) list
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

    let private renderUnion (s: RuntimeSurface) (spec: UnionTypeSpec) : string =
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
                    discriminatorMethod s (parseBody discBody)
                )
                .AddAttributeLists(supportAttr s (spec.Layouts |> List.map (fun l -> l.Range)))
                .AddAttributeLists(unionAttr s)

        renderUnit s s.Namespace spec.Name (unionUsings s cases) shell

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
        p.Layouts |> List.exists (fun l -> readEntriesLines s l.Entries |> hasReadError)

    let rec private wireNamedRefs (w: WireType) : string list =
        match w with
        | Named n -> [ n ]
        | EnumRef(n, _) -> [ n ]
        | Array(item, cnt) ->
            wireNamedRefs item
            @ (match cnt with
               | TypedCount t -> wireNamedRefs t
               | _ -> [])
        | Option inner -> wireNamedRefs inner
        | RegistryHolder inner -> wireNamedRefs inner
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
        | TOption inner
        | THolder inner -> apiNamedRefs inner
        | TNamed n -> [ n ]
        | TUnion n -> [ n ]
        | TEnum n -> [ n ]
        | _ -> []

    /// Named types the delivered output can resolve: runtime-provided primitives plus generated
    /// named/bitflags/union types that are themselves stub-free and reference only resolvable
    /// types (fixpoint). A union is a candidate like any other type — it is generated now, so a
    /// union reference resolves as soon as every type its arms read does.
    let private resolvableTypes (s: RuntimeSurface) (protocol: ProtocolSpec) : Set<string> =
        let runtimeProvided = Set.ofList [ "Position" ]

        let stubFreeEntries (entries: WireEntry list) =
            readEntriesLines s entries |> hasReadError |> not

        let candidates =
            [
                for t in protocol.Types ->
                    let refs =
                        (t.ApiFields |> List.collect (fun f -> apiNamedRefs f.Type))
                        @ (t.Layouts |> List.collect (fun l -> l.Entries |> List.collect entryNamedRefs))

                    t.Name, refs, t.Layouts |> List.forall (fun l -> stubFreeEntries l.Entries)
                for u in protocol.Unions ->
                    let arms = u.Layouts |> List.collect (fun l -> l.Arms)

                    let refs = arms |> List.collect (fun a -> a.Entries |> List.collect entryNamedRefs)

                    u.Name, refs, arms |> List.forall (fun a -> stubFreeEntries a.Entries)
            ]

        let mutable known =
            Set.unionMany
                [
                    runtimeProvided
                    protocol.Bitflags |> List.map (fun b -> b.Name) |> Set.ofList
                    protocol.Enums |> List.map (fun e -> e.Name) |> Set.ofList
                ]

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

                        if not slice.IsEmpty then
                            yield st, dir, slice
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

        // ----- the flat lookup: every per-run table concatenated once, addressed by arithmetic -----
        // The tables above are keyed by (phase, direction) and a protocol-version *range*, which a
        // switch plus a chain of range compares can answer but only in time proportional to the
        // number of ranges. Concatenating them into one blob and indexing it by
        // ((int)phase * directions + (int)direction) * pvCount + (pv - minPv) answers the same
        // question with two loads and no branching on the version at all.
        let dirCount = s.DirectionOrder.Length
        let slotCount = s.PhaseOrder.Length * dirCount

        let slotOf (st: ProtocolState) (dir: Direction) =
            let phaseIndex = s.PhaseOrder |> List.findIndex ((=) st)
            let dirIndex = s.DirectionOrder |> List.findIndex ((=) dir)
            phaseIndex * dirCount + dirIndex

        let allRuns =
            [
                for st, dir, slice in slices do
                    for lo, hi, map in runsFor slice do
                        yield st, dir, lo, hi, map
            ]

        let minPv = allRuns |> List.map (fun (_, _, lo, _, _) -> lo) |> List.min
        let maxPv = allRuns |> List.map (fun (_, _, _, hi, _) -> hi) |> List.max
        let pvCount = maxPv - minPv + 1

        let blob = ResizeArray<int>()
        let offsets = Array.zeroCreate<int> (slotCount * pvCount)
        let lengths = Array.zeroCreate<int> (slotCount * pvCount)

        for st, dir, lo, hi, map in allRuns do
            let maxId = map |> List.map fst |> List.max
            let table = Array.create (maxId + 1) -1

            for id, ordinal in map do
                table.[id] <- ordinal

            let offset = blob.Count
            blob.AddRange table

            for pv in lo..hi do
                let index = slotOf st dir * pvCount + (pv - minPv)
                offsets.[index] <- offset
                lengths.[index] <- table.Length

        let window =
            [
                for i in 0 .. offsets.Length - 1 do
                    yield offsets.[i]
                    yield lengths.[i]
            ]

        line "    /// <summary>Number of members of the phase and direction enums the tables were built"
        line "    /// against. Public because a caller that indexes anything by (phase, direction) must"
        line "    /// size it from the same numbers rather than reflecting over the enums.</summary>"
        line (sprintf "    public const int PhaseCount = %d;" s.PhaseOrder.Length)
        line ""
        line (sprintf "    public const int DirectionCount = %d;" dirCount)
        line ""
        line (sprintf "    public const int CatalogCount = %d;" slotCount)
        line ""
        line (sprintf "    private const int MinPv = %d;" minPv)
        line ""
        line (sprintf "    private const int PvCount = %d;" pvCount)
        line ""
        line "    /// <summary>Every per-run id->ordinal table, concatenated. -1 marks an id this run"
        line "    /// does not map.</summary>"
        line "    private static ReadOnlySpan<short> OrdinalBlob =>"
        line (sprintf "        [%s];" (blob |> Seq.map string |> String.concat ", "))
        line ""
        line "    /// <summary>Offset and length, interleaved, of the table for one (phase, direction,"
        line "    /// protocol version) inside <see cref=\"OrdinalBlob\"/>: the pair sits in one cache line"
        line "    /// so the lookup reads both with a single probe. Length 0 means that combination"
        line "    /// carries no packets.</summary>"
        line "    private static ReadOnlySpan<int> TableWindow =>"
        line (sprintf "        [%s];" (window |> Seq.map string |> String.concat ", "))
        line ""

        line (
            sprintf
                "    public static bool TryGetOrdinal(int id, int %s, %s phase, %s dir, out ushort ordinal)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "    {"
        line (sprintf "        var pvIndex = %s - MinPv;" s.VersionParam)
        line "        // phase and direction are bounded separately on purpose: a single check on the"
        line "        // combined slot would let an out-of-range direction alias onto another phase's row."
        line "        if ((uint)pvIndex < PvCount && (uint)phase < PhaseCount && (uint)dir < DirectionCount)"
        line "        {"
        line "            var window = (((int)phase * DirectionCount + (int)dir) * PvCount + pvIndex) * 2;"
        line "            if ((uint)id < (uint)TableWindow[window + 1])"
        line "            {"
        line "                var value = OrdinalBlob[TableWindow[window] + id];"
        line "                if (value >= 0)"
        line "                {"
        line "                    ordinal = (ushort)value;"
        line "                    return true;"
        line "                }"
        line "            }"
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

        line (
            sprintf
                "    public static ReadOnlySpan<PacketDescriptor> Catalog(%s phase, %s dir)"
                s.PhaseEnum
                s.DirectionEnum
        )

        line "    {"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (
                sprintf "            case (%s.%A, %s.%A): return Catalog%A%A;" s.PhaseEnum st s.DirectionEnum dir st dir
            )

        line "        }"
        line ""
        line "        return default;"
        line "    }"
        line "}"
        sb.ToString()

    /// `Flow/PacketFlow.g.cs`: one lookup + one ordinal jump table + one constrained call per
    /// packet. Dispatch is deliberately synchronous: the decode must finish before the next
    /// transport read (the `IncomingPacket.Body` window); anything async happens in the facade after.
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
                            Registry.slice st dir entries |> List.filter (fun e -> dispatchable e.Spec)

                        if not slice.IsEmpty then
                            yield st, dir, slice
            ]

        // A `.g.cs` file is auto-generated as far as the compiler is concerned, so the project's
        // `<Nullable>enable</Nullable>` does not reach it and every `?` here would be both a
        // CS8669 warning and a dead annotation. The Try doors depend on the annotation being
        // live: `[NotNullWhen(true)] out IPacket?` is what makes a caller who reads the packet
        // after a false return get a warning instead of a null.
        line "#nullable enable"
        line ""
        line (sprintf "using %s;" s.UsingSystem)
        line (sprintf "using %s;" s.UsingSerialization)
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""

        line (
            sprintf "public delegate void TrailingBytesHook(int packetId, int %s, long remainingBytes);" s.VersionParam
        )

        line ""
        line "/// <summary>Generated dispatcher. Packets whose codegen is still stubbed are not"
        line "/// dispatched — they fall through to <c>Unknown</c> instead of throwing inside the"
        line "/// receive loop. Trailing bytes raise a hook, not an exception: the packet already"
        line "/// reached the visitor, but the spec is suspect."
        line "/// Three doors onto the same table: <c>Dispatch</c> (throws on a malformed body),"
        line "/// <c>TryDispatch</c> (same visitor, a false + reason instead of the throw) and"
        line "/// <c>TryDecode</c> (visitor-free — hands back the decoded packet itself).</summary>"
        line "public static partial class PacketFlow"
        line "{"
        line "    public static event TrailingBytesHook? OnTrailingBytes;"
        line ""
        line "    /// <summary>Raises <see cref=\"OnTrailingBytes\"/> for a caller that decoded the body"
        line "    /// itself. An event can only be raised inside the type that declares it, and the"
        line "    /// generated handlers decode without going through <see cref=\"Dispatch\"/>; the hook"
        line "    /// stays the one place a suspect spec is reported from.</summary>"

        line (
            sprintf
                "    internal static void RaiseTrailingBytes(int packetId, int %s, long remainingBytes) => OnTrailingBytes?.Invoke(packetId, %s, remainingBytes);"
                s.VersionParam
                s.VersionParam
        )

        line ""

        line (
            sprintf
                "    public static void Dispatch<TVisitor>(in IncomingPacket raw, int %s, %s phase, %s dir, ref TVisitor visitor)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "        where TVisitor : IPacketVisitor"
        line "    {"

        line (
            sprintf "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, dir, out var ordinal))" s.VersionParam
        )

        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return;"
        line "        }"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        bool handled;"
        line "        // The jump table is shared with the Try door, which must tell a failed body read"
        line "        // from an exception thrown by the visitor: the table lowers this flag once the"
        line "        // body is decoded, right before it calls the visitor. Dispatch converts nothing,"
        line "        // so here the flag is written and never read."
        line "        bool reading = true;"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (sprintf "            case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)

            line (
                sprintf
                    "                handled = Dispatch%A%A(ordinal, ref %s, %s, ref visitor, ref reading);"
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

        line (
            sprintf "            OnTrailingBytes?.Invoke(raw.Id, %s, %s.RemainingCount);" s.VersionParam s.ReaderParam
        )

        line "    }"

        // ----- TryDispatch: the same table, a reason instead of a throw -----
        line ""
        line "    /// <summary>Dispatch that survives a malformed body: returns false with"
        line "    /// <paramref name=\"error\" /> filled where <see cref=\"Dispatch\" /> would let the"
        line "    /// exception out. True means the packet reached the visitor — including the normal"
        line "    /// stream condition of an id this (phase, direction) has no mapping for, which"
        line "    /// reaches <c>Unknown</c> exactly as in <see cref=\"Dispatch\" />. Trailing bytes stay"
        line "    /// a hook, not a failure. Only a failure of the body read is converted, and only the"
        line "    /// kinds a decoder may swallow (see <c>TryClassify</c>): cancellation, a stubbed"
        line "    /// decoder and out-of-memory still propagate. An exception thrown by the visitor"
        line "    /// itself is never converted — the table lowers <c>reading</c> before it calls the"
        line "    /// visitor, so the consumer's own bugs come out as themselves.</summary>"

        line (
            sprintf
                "    public static bool TryDispatch<TVisitor>(in IncomingPacket raw, int %s, %s phase, %s direction, ref TVisitor visitor, out DecodeError error)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "        where TVisitor : IPacketVisitor"
        line "    {"
        line "        error = DecodeError.None;"
        line ""

        line (
            sprintf
                "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, direction, out var ordinal))"
                s.VersionParam
        )

        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return true;"
        line "        }"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        bool handled;"
        line "        // True while the body is being read; the table lowers it right before it hands the"
        line "        // packet to the visitor. The filter below tests it, so an exception out of the"
        line "        // visitor is not mistaken for a malformed packet."
        line "        bool reading = true;"
        line "        try"
        line "        {"
        line "            switch (phase, direction)"
        line "            {"

        for st, dir, _ in slices do
            line (sprintf "                case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)

            line (
                sprintf
                    "                    handled = Dispatch%A%A(ordinal, ref %s, %s, ref visitor, ref reading);"
                    st
                    dir
                    s.ReaderParam
                    s.VersionParam
            )

            line "                    break;"

        line "                default:"
        line "                    handled = false;"
        line "                    break;"
        line "            }"
        line "        }"
        line "        catch (Exception ex) when (reading && TryClassify(ex, out var reason))"
        line "        {"
        line "            error = reason;"
        line "            return false;"
        line "        }"
        line ""
        line "        if (!handled)"
        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return true;"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)

        line (
            sprintf "            OnTrailingBytes?.Invoke(raw.Id, %s, %s.RemainingCount);" s.VersionParam s.ReaderParam
        )

        line ""
        line "        return true;"
        line "    }"

        // ----- TryDecode: the visitor-free door, only when packets carry a non-generic type -----
        match s.PacketBaseInterface with
        | Some baseIface ->
            line ""
            line "    /// <summary>One raw packet in, one decoded packet out — no visitor to write."
            line (sprintf "    /// An id this (phase, direction) cannot map yields an <see cref=\"UnknownPacket\" />")
            line "    /// and still returns true: an unmapped id is a normal stream condition, not an error."
            line "    /// A malformed body returns false with <paramref name=\"error\" /> filled and"
            line "    /// <paramref name=\"packet\" /> null. The allocation-free hot path is"
            line "    /// <see cref=\"Dispatch\" /> / <see cref=\"TryDispatch\" />; this door costs nothing"
            line "    /// extra either — packets are classes, so the capture is a reference, not a box.</summary>"

            line (
                sprintf
                    "    public static bool TryDecode(in IncomingPacket raw, int %s, %s phase, %s direction, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out %s? packet, out DecodeError error)"
                    s.VersionParam
                    s.PhaseEnum
                    s.DirectionEnum
                    baseIface
            )

            line "    {"
            line "        var capture = new Capture(phase, direction);"
            line ""

            line (
                sprintf "        if (!TryDispatch(in raw, %s, phase, direction, ref capture, out error))" s.VersionParam
            )

            line "        {"
            line "            packet = null;"
            line "            return false;"
            line "        }"
            line ""
            line "        packet = capture.Result!;"
            line "        return true;"
            line "    }"
            line ""

            line (
                sprintf
                    "    /// <summary>Keeps the decoded packet as <see cref=\"%s\" />. The assignment is a"
                    baseIface
            )

            line "    /// reference conversion (every packet is a class that implements it), so there is no"
            line "    /// boxing and no adapter object — the same jump table, one field write.</summary>"
            line "    private struct Capture : IPacketVisitor"
            line "    {"
            line (sprintf "        private readonly %s _phase;" s.PhaseEnum)
            line (sprintf "        private readonly %s _direction;" s.DirectionEnum)
            line ""
            line (sprintf "        public %s? Result;" baseIface)
            line ""
            line (sprintf "        public Capture(%s phase, %s direction)" s.PhaseEnum s.DirectionEnum)
            line "        {"
            line "            _phase = phase;"
            line "            _direction = direction;"
            line "            Result = null;"
            line "        }"
            line ""

            line (
                sprintf
                    "        public void Visit<T>(T packet) where T : class, %s<T> => Result = (%s)packet;"
                    (s.PacketInterface |> Option.defaultValue baseIface)
                    baseIface
            )

            line ""

            line
                "        public void Unknown(in IncomingPacket raw) => Result = new UnknownPacket(raw.Id, _phase, _direction);"

            line "    }"
        | None -> ()

        // ----- the one place that decides what a Try door is allowed to swallow -----
        line ""
        line "    /// <summary>Maps an exception raised while reading a packet body onto a"
        line "    /// <see cref=\"DecodeError\" />. Returns false for exceptions a decoder must never"
        line "    /// swallow — used as an exception filter, so those propagate without unwinding."
        line "    /// <para>"
        line "    /// <c>ArgumentException</c> is deliberately NOT on the propagate list. Bytes off the"
        line "    /// wire reach it: a compound with a duplicate key ends in <c>Dictionary.Add</c> inside"
        line "    /// <c>NbtCompound.Add</c>, and an unnamed tag in a compound throws there too. Those are"
        line "    /// data errors, so they are <c>Malformed</c>. Only an exception the caller's own code"
        line "    /// raised should escape a Try door, and that case is handled before the filter runs:"
        line "    /// the jump table lowers <c>reading</c> before it calls the visitor."
        line "    /// </para></summary>"
        line "    private static bool TryClassify(Exception ex, out DecodeError error)"
        line "    {"
        line "        switch (ex)"
        line "        {"
        line "            case ProtocolNotSupportException _:"
        line "            case NotSupportedException _:"
        line "                error = DecodeError.UnsupportedVersion;"
        line "                return true;"
        line "            case OperationCanceledException _:"
        line "            case NotImplementedException _:"
        line "            case OutOfMemoryException _:"
        line "                error = DecodeError.None;"
        line "                return false;"
        line "            default:"
        line "                error = DecodeError.Malformed;"
        line "                return true;"
        line "        }"
        line "    }"

        for st, dir, slice in slices do
            line ""
            line (sprintf "    /// <summary>One ordinal, one read, one constrained call. <paramref name=\"reading\" />")
            line "    /// goes false between the two: above it the exception is the packet's fault, below it"
            line "    /// the visitor's. Only the Try door reads it.</summary>"

            line (
                sprintf
                    "    private static bool Dispatch%A%A<TVisitor>(ushort ordinal, ref %s %s, int %s, ref TVisitor visitor, ref bool reading)"
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
                line "            {"

                line (
                    sprintf
                        "                var packet = %s.Read(ref %s, %s);"
                        (relTypeName e.Spec)
                        s.ReaderParam
                        s.VersionParam
                )

                line "                reading = false;"
                line "                visitor.Visit(packet);"
                line "                return true;"
                line "            }"
                line ""

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
            entries |> List.map (fun e -> shortName e.Spec) |> List.countBy id |> Map.ofList

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

    /// `Flow/<Direction>Handler.g.cs`: one base for every phase of one direction, phase slot led
    /// by the consumer, ValueTask handlers awaited by the facade after the synchronous dispatch.
    ///
    /// `HandleAsync` resolves the ordinal itself and reads the packet inside the case block that
    /// already knows its type, so the decoded packet reaches `On<Name>` with nothing dynamic in
    /// between. The handler is deliberately NOT an `IPacketVisitor`: routing it through
    /// `PacketFlow` would hand the visitor's `Visit<T>` a `ValueTask` it has nowhere to return,
    /// and a dropped `ValueTask` is a silently lost continuation. `PacketFlow` and
    /// `IPacketVisitor` remain for callers that want them — `PacketSubscriptions` is one — and
    /// are emitted unchanged.
    let private renderHandlerFile
        (s: RuntimeSurface)
        (dispatchable: PacketSpec -> bool)
        (dir: Direction)
        (className: string)
        (entries: Registry.CatalogEntry list)
        : string
        =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    let slice =
                        Registry.slice st dir entries |> List.filter (fun e -> dispatchable e.Spec)

                    if not slice.IsEmpty then
                        yield st, slice
            ]

        let names = handlerNames (slices |> List.collect snd)

        // The phase a connection is in when the first packet of this direction can arrive: a
        // client starts listening in Login, a server starts reading in Handshaking.
        let defaultPhase =
            match dir with
            | Clientbound -> "Login"
            | Serverbound -> "Handshaking"

        let lowerDir = (sprintf "%A" dir).ToLowerInvariant()

        line "using System.Threading.Tasks;"
        line (sprintf "using %s;" s.UsingSerialization)
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line (sprintf "/// <summary>Generated handler base over every %s phase. The truth about" lowerDir)
        line "/// the current phase is the consumer's: set <see cref=\"Phase\"/> as the connection"
        line "/// advances. <c>HandleAsync</c> decodes synchronously (the raw data window must not"
        line "/// cross an await) and awaits the handler's result after. <c>OnUnknown</c> must not"
        line "/// hold on to <c>raw</c> beyond the call.</summary>"
        line (sprintf "public abstract partial class %s" className)
        line "{"
        line (sprintf "    public %s Phase { get; protected set; } = %s.%s;" s.PhaseEnum s.PhaseEnum defaultPhase)
        line ""
        line (sprintf "    protected static %s Direction => %s.%A;" s.DirectionEnum s.DirectionEnum dir)
        line ""

        line "    /// <summary>The registry lookup and the typed read happen here, in a case block where"
        line "    /// the packet type is statically known, so nothing between the wire and"
        line "    /// <c>On&lt;Name&gt;</c> is dynamic. <see cref=\"Phase\"/> is read once: a handler that"
        line "    /// advances the phase does so after the switch, and this packet is read as the phase"
        line "    /// it arrived in.</summary>"
        line (sprintf "    public ValueTask HandleAsync(in IncomingPacket raw, int %s)" s.VersionParam)
        line "    {"
        line "        var phase = Phase;"

        line (
            sprintf
                "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, %s.%A, out var ordinal))"
                s.VersionParam
                s.DirectionEnum
                dir
        )

        line "            return OnUnknown(in raw);"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        ValueTask pending;"
        line "        switch (phase)"
        line "        {"

        for st, slice in slices do
            line (sprintf "            case %s.%A:" s.PhaseEnum st)
            line "                switch (ordinal)"
            line "                {"

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line (sprintf "                    case %d:" e.Ordinal)
                line "                    {"

                line (
                    sprintf
                        "                        var packet = %s.Read(ref %s, %s);"
                        (relTypeName e.Spec)
                        s.ReaderParam
                        s.VersionParam
                )

                line (sprintf "                        pending = %s(packet);" handler)
                line "                        break;"
                line "                    }"

            line ""
            line "                    default:"
            line "                        return OnUnknown(in raw);"
            line "                }"
            line ""
            line "                break;"

        line "            default:"
        line "                return OnUnknown(in raw);"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)

        line (
            sprintf
                "            PacketFlow.RaiseTrailingBytes(raw.Id, %s, %s.RemainingCount);"
                s.VersionParam
                s.ReaderParam
        )

        line ""
        line "        return pending;"
        line "    }"

        line ""
        line "    protected virtual ValueTask OnUnknown(in IncomingPacket raw) => default;"

        for st, slice in slices do
            line ""
            line (sprintf "    // --- %A ---" st)

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line ""
                line (sprintf "    protected virtual ValueTask %s(%s packet) => default;" handler (relTypeName e.Spec))

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
                Contents =
                    renderRawUnit
                        "ClientboundHandler"
                        (renderHandlerFile s dispatchable Clientbound "ClientboundHandler" entries)
            }
            {
                RelativePath = "Flow/ServerboundHandler.g.cs"
                Contents =
                    renderRawUnit
                        "ServerboundHandler"
                        (renderHandlerFile s dispatchable Serverbound "ServerboundHandler" entries)
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
            member _.RenderEnum spec = renderEnum surface spec
            member _.RenderUnion spec = renderUnion surface spec
            member _.RenderPacket entry = renderPacket surface entry
            member _.RenderProtocol entries = renderProtocolExtras surface entries
        }

    /// The default C# target: the McProtoNet runtime surface.
    let target: ILanguageTarget = targetFor mcProtoNet
