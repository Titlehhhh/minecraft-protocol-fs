namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module PacketLayers =

    open type SyntaxFactory
    open Text
    open Structure
    open Statements
    open Bodies
    open NamedTypes
    open PacketParts
    open Json

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
            Json: (string * JsonShape) list
        }

    /// Mechanical group name from a layout range: `V759`, `V761_763`, `V764_Last`. Shared with the
    /// union backend, which labels its per-layer cases the same way — one naming scheme, not two.
    let internal layerName (allRanges: VersionRange list) (r: VersionRange) : string =
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
            | InlineUnion(_, arms) -> arms |> List.collect (fun arm -> boundApis arm.Entries)
            | _ -> [])

    /// A field is common when it lives in every version (`Present = All`).
    let private isCommon (f: ApiField) = f.Present = All

    /// Per-layer C# type and JSON shape of a group field: existence-optionality (`TOption` because the field is
    /// absent in other versions) is stripped; the layer's own wire decides real nullability.
    let private layerField (s: RuntimeSurface) (l: WireLayout) (f: ApiField) : string * JsonShape =
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
                | InlineUnion(_, arms) ->
                    arms
                    |> List.exists (fun arm ->
                        boundApis arm.Entries |> List.contains f.Name || optionalIn arm.Entries)
                | _ -> false)

        let optionalHere = optionalIn l.Entries

        if optionalHere then
            csType s inner + "?", JOption(shapeOfApi inner)
        else
            csType s inner, shapeOfApi inner

    /// Cut a multi-layout packet into layers. A layer with no non-common fields gets no group.
    let private packetLayers (s: RuntimeSurface) (p: PacketSpec) : PacketLayer list =
        let commonNames =
            p.ApiFields |> List.filter isCommon |> List.map (fun f -> f.Name) |> Set.ofList

        [
            for l in p.Layouts ->
                let bound = boundApis l.Entries |> Set.ofList

                let own =
                    p.ApiFields
                    |> List.filter (fun f -> not (commonNames.Contains f.Name) && bound.Contains f.Name)
                    |> List.map (fun f -> f.Name, layerField s l f)

                let fields = own |> List.map (fun (n, (t, _)) -> t, n)

                {
                    Layout = l
                    GroupName =
                        (if fields.IsEmpty then
                             None
                         else
                             Some(layerName (p.Layouts |> List.map (fun l -> l.Range)) l.Range))
                    Fields = fields
                    Json = own |> List.map (fun (n, (_, shape)) -> n, shape)
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
    let internal renderPacket (s: RuntimeSurface) (e: Registry.CatalogEntry) : string =
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
                            Json = []
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

        // JSON view: common fields, then the fields of whichever layer the value holds, all in one
        // flat object. The layer is a fact about the value, not a level of the JSON.
        let jsonBody =
            let w = s.JsonWriterParam

            [
                yield sprintf "%s.WriteStartObject();" w

                for f in commonFields do
                    yield! propertyLines s f.Name (shapeOfApi f.Type) f.Name

                // `else if`: a hand-built packet with two layers set must not write a key twice.
                let grouped = layers |> List.filter (fun l -> l.GroupName.IsSome)

                for i, l in List.indexed grouped do
                    match l.GroupName with
                    | Some g ->
                        let v = camel g
                        yield sprintf "%sif (%s is { } %s)" (if i = 0 then "" else "else ") g v
                        yield "{"

                        for n, shape in l.Json do
                            yield! propertyLines s n shape (sprintf "%s.%s" v n)

                        yield "}"
                    | None -> ()

                yield sprintf "%s.WriteEndObject();" w
            ]

        let identityMembers =
            identityMember s e
            :: [ for i in Option.toList s.PacketBaseInterface -> identityBaseMember s i ]

        let shell =
            (packetRecordShell s.PacketInterface s.PacketBaseInterface p.ClassName commonPos layers)
                .AddMembers(
                    readMethod s p.ClassName (parseBody readBody),
                    writeMethod s false (parseBody writeBody),
                    writeJsonMethod s false (parseBody jsonBody)
                )
                .AddMembers(identityMembers @ packetIdMethods s |> List.toArray)
                .AddAttributeLists(supportAttr s (p.Layouts |> List.map (fun l -> l.Range)))
                .AddAttributeLists(
                    packetAttr s e :: packetFieldAttrs s commonFields (if multi then layers else [])
                    |> List.toArray
                )

        renderUnit s (packetNamespace s p) p.ClassName (usingsFor s p.ApiFields) shell
