namespace McProtocol.Codegen

open System
open McProtocol.Dsl

/// C# backend shaped like McProtoNet's `McProtoNet.Protocol.Types`:
///   [ProtocolSupport(from, to)] on the type, `readonly partial record struct` for value types and
///   `sealed partial class` for reference types, in namespace McProtoNet.Protocol. Read/Write live
///   *together* in the type body (Read as a static factory, Write as an instance method) against the
///   MinecraftPrimitiveReader (ref struct) / MinecraftPrimitiveWriter surface.
module CSharp =

    [<Literal>]
    let private Ns = "McProtoNet.Protocol"

    // ----- api model type -> C# type -----
    let rec private csType (t: ApiType) : string =
        match t with
        | TBool -> "bool"
        | TInt -> "int"
        | TLong -> "long"
        | TFloat -> "float"
        | TDouble -> "double"
        | TString -> "string"
        | TUuid -> "Guid"
        | TNbt -> "NbtTag"
        | TBytes -> "byte[]"
        | TArray inner -> csType inner + "[]"
        | TOption inner -> csType inner + "?"
        | TNamed n -> n
        | TUnion n -> n

    /// Value types become `record struct`; everything else a `sealed class` (mirrors McProtoNet).
    let private isValue =
        function
        | TBool | TInt | TLong | TFloat | TDouble | TUuid -> true
        | _ -> false

    // ----- MinecraftPrimitiveReader / MinecraftPrimitiveWriter method surface -----
    let private readerCall (w: WireType) : Result<string, string> =
        match w with
        | Bool -> Ok "ReadBoolean()"
        | VarInt -> Ok "ReadVarInt()"
        | VarLong -> Ok "ReadVarLong()"
        | I8 -> Ok "ReadSignedByte()"
        | U8 -> Ok "ReadUnsignedByte()"
        | I16 -> Ok "ReadSignedShort()"
        | U16 -> Ok "ReadUnsignedShort()"
        | I32 -> Ok "ReadSignedInt()"
        | U32 -> Ok "ReadUnsignedInt()"
        | I64 -> Ok "ReadSignedLong()"
        | U64 -> Ok "ReadUnsignedLong()"
        | F32 -> Ok "ReadFloat()"
        | F64 -> Ok "ReadDouble()"
        | Str -> Ok "ReadString()"
        | Uuid -> Ok "ReadUUID()"
        | Nbt -> Ok "ReadNbtTag(false)"
        | AnonNbt -> Ok "ReadNbtTag(true)"
        | Named n -> Ok(sprintf "ReadType<%s>(protocolVersion)" n)
        | other -> Error(sprintf "%A" other)

    let private writerCall (w: WireType) (value: string) : Result<string, string> =
        let c m = Ok(sprintf "%s(%s)" m value)
        match w with
        | Bool -> c "WriteBoolean"
        | VarInt -> c "WriteVarInt"
        | VarLong -> c "WriteVarLong"
        | I8 -> c "WriteSignedByte"
        | U8 -> c "WriteUnsignedByte"
        | I16 -> c "WriteSignedShort"
        | U16 -> c "WriteUnsignedShort"
        | I32 -> c "WriteSignedInt"
        | U32 -> c "WriteUnsignedInt"
        | I64 -> c "WriteSignedLong"
        | U64 -> c "WriteUnsignedLong"
        | F32 -> c "WriteFloat"
        | F64 -> c "WriteDouble"
        | Str -> c "WriteString"
        | Uuid -> c "WriteUUID"
        | Nbt -> c "WriteNbt"
        | AnonNbt -> c "WriteNbt"
        | Named n -> Ok(sprintf "WriteType<%s>(%s, protocolVersion)" n value)
        | other -> Error(sprintf "%A" other)

    let private camel (s: string) =
        if s.Length = 0 || s.[0] = '_' then s
        else string (Char.ToLower s.[0]) + s.[1..]

    let private pascal (s: string) =
        if s.Length = 0 then s else string (Char.ToUpper s.[0]) + s.[1..]

    /// `[ProtocolSupport(from, to)]` from the union of a set of version ranges.
    let private supportAttrOf (ranges: VersionRange list) =
        let lo, hi = VersionRangeX.span (if List.isEmpty ranges then [ All ] else ranges)
        let loS = lo |> Option.map string |> Option.defaultValue "MinecraftVersion.StartProtocol"
        let hiS = hi |> Option.map string |> Option.defaultValue "MinecraftVersion.LatestProtocol"
        sprintf "[ProtocolSupport(%s, %s)]" loS hiS

    let private supportAttr (spec: NamedTypeSpec) =
        supportAttrOf (spec.Layouts |> List.map (fun l -> l.Range))

    /// Reader call, writer method and C# local type for a bitflags backing integer.
    let private backingInfo (w: WireType) : (string * string * string) option =
        match w with
        | U8 -> Some("ReadUnsignedByte()", "WriteUnsignedByte", "byte")
        | U16 -> Some("ReadUnsignedShort()", "WriteUnsignedShort", "ushort")
        | U32 -> Some("ReadUnsignedInt()", "WriteUnsignedInt", "uint")
        | U64 -> Some("ReadUnsignedLong()", "WriteUnsignedLong", "ulong")
        | VarInt -> Some("ReadVarInt()", "WriteVarInt", "int")
        | VarLong -> Some("ReadVarLong()", "WriteVarLong", "long")
        | I8 -> Some("ReadSignedByte()", "WriteSignedByte", "sbyte")
        | I16 -> Some("ReadSignedShort()", "WriteSignedShort", "short")
        | I32 -> Some("ReadSignedInt()", "WriteSignedInt", "int")
        | I64 -> Some("ReadSignedLong()", "WriteSignedLong", "long")
        | _ -> None

    /// A version range as a C# boolean condition, or None when it always applies.
    let private guardCondition (range: VersionRange) : string option =
        match VersionRangeX.bounds range with
        | None, None -> None
        | Some lo, None -> Some(sprintf "protocolVersion >= %d" lo)
        | None, Some hi -> Some(sprintf "protocolVersion <= %d" hi)
        | Some lo, Some hi -> Some(sprintf "protocolVersion >= %d && protocolVersion <= %d" lo hi)

    /// Wire Read entries of the type's (single) layout, paired as (apiName, wireType).
    let private reads (spec: NamedTypeSpec) : (string * WireType) list =
        spec.Layouts
        |> List.tryFind (fun l -> not (List.isEmpty l.Entries))
        |> Option.map (fun l -> l.Entries |> List.choose (function Read (_, wt, api) -> Some(api, wt) | _ -> None))
        |> Option.defaultValue []

    let private nonReadEntries (spec: NamedTypeSpec) =
        spec.Layouts
        |> List.collect (fun l -> l.Entries)
        |> List.filter (function Read _ -> false | _ -> true)

    // ----- assembly -----
    let private usings (spec: NamedTypeSpec) =
        let text = spec.ApiFields |> List.map (fun f -> csType f.Type) |> String.concat " "
        [ yield "McProtoNet.Protocol.Attributes"
          yield "McProtoNet.Serialization"
          if text.Contains "NbtTag" then yield "McProtoNet.NBT"
          if text.Contains "Guid" then yield "System" ]

    let private readBody (spec: NamedTypeSpec) : string list =
        let bound = reads spec |> List.map (fst >> camel) |> Set.ofList
        let readLines =
            reads spec
            |> List.map (fun (api, wt) ->
                match readerCall wt with
                | Ok call -> sprintf "        var %s = reader.%s;" (camel api) call
                | Error e -> sprintf "        // TODO(codegen): read '%s' (%s)" api e)
        let todos = nonReadEntries spec |> List.map (fun e -> sprintf "        // TODO(codegen): %A" e)
        let ctorArgs =
            spec.ApiFields
            |> List.map (fun f -> if bound.Contains(camel f.Name) then camel f.Name else "default")
            |> String.concat ", "
        readLines @ todos @ [ sprintf "        return new %s(%s);" spec.Name ctorArgs ]

    let private writeBody (spec: NamedTypeSpec) : string list =
        reads spec
        |> List.map (fun (api, wt) ->
            match writerCall wt api with
            | Ok call -> sprintf "        writer.%s;" call
            | Error e -> sprintf "        // TODO(codegen): write '%s' (%s)" api e)

    let private render (spec: NamedTypeSpec) : string =
        let name = spec.Name
        let value = spec.ApiFields |> List.forall (fun f -> isValue f.Type)
        let lines = ResizeArray<string>()
        let add (s: string) = lines.Add s

        for u in usings spec do
            add (sprintf "using %s;" u)
        add ""
        add (sprintf "namespace %s;" Ns)
        add ""
        if spec.Layouts.Length > 1 then
            add (sprintf "// TODO(codegen): %d version layouts; only the first is emitted." spec.Layouts.Length)
        add (supportAttr spec)

        let header =
            if value then
                let ps = spec.ApiFields |> List.map (fun f -> sprintf "%s %s" (csType f.Type) f.Name) |> String.concat ", "
                sprintf "public readonly partial record struct %s(%s)" name ps
            else
                sprintf "public sealed partial class %s" name
        add header
        add "{"

        // reference types spell out properties + a constructor (value types get them positionally)
        if not value then
            for f in spec.ApiFields do
                add (sprintf "    public %s %s { get; }" (csType f.Type) f.Name)
            add ""
            let ps = spec.ApiFields |> List.map (fun f -> sprintf "%s %s" (csType f.Type) (camel f.Name)) |> String.concat ", "
            add (sprintf "    public %s(%s)" name ps)
            add "    {"
            for f in spec.ApiFields do
                add (sprintf "        %s = %s;" f.Name (camel f.Name))
            add "    }"
            add ""

        add (sprintf "    public static %s Read(ref MinecraftPrimitiveReader reader, int protocolVersion)" name)
        add "    {"
        add (sprintf "        ThrowHelper.ThrowIfProtocolNotSupported<%s>(protocolVersion);" name)
        for l in readBody spec do add l
        add "    }"
        add ""
        let writeSig =
            if value then "    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)"
            else "    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)"
        add writeSig
        add "    {"
        add (sprintf "        ThrowHelper.ThrowIfProtocolNotSupported<%s>(protocolVersion);" name)
        for l in writeBody spec do add l
        add "    }"
        add "}"
        add ""
        String.Join("\n", lines)

    let private renderBitflags (spec: BitflagsSpec) : string =
        let name = spec.Name
        let apiFlags = spec.Layouts |> List.collect (fun l -> l.Flags) |> List.distinct
        let layouts = spec.Layouts
        let lastIdx = List.length layouts - 1

        let readCore (l: BitflagsLayout) =
            match backingInfo l.Backing with
            | None -> [ sprintf "        // TODO(codegen): backing %A" l.Backing; "        return default;" ]
            | Some (readCall, _, cType) ->
                let args =
                    apiFlags
                    |> List.map (fun f ->
                        match List.tryFindIndex ((=) f) l.Flags with
                        | Some i -> sprintf "(flags & (1 << %d)) != 0" i
                        | None -> "false")
                    |> String.concat ", "
                [ sprintf "        %s flags = reader.%s;" cType readCall
                  sprintf "        return new %s(%s);" name args ]

        let writeCore last (l: BitflagsLayout) =
            match backingInfo l.Backing with
            | None -> [ sprintf "        // TODO(codegen): backing %A" l.Backing ]
            | Some (_, writeMethod, cType) ->
                [ yield sprintf "        %s flags = 0;" cType
                  for i, f in List.indexed l.Flags -> sprintf "        if (%s) flags |= (1 << %d);" (pascal f) i
                  yield sprintf "        writer.%s(flags);" writeMethod
                  if not last then yield "        return;" ]

        let multi = lastIdx > 0
        let indent lines = lines |> List.map (fun c -> "    " + c)
        let wrap (guard: string option) (core: string list) =
            match guard with
            | Some cond -> [ sprintf "        if (%s)" cond; "        {" ] @ indent core @ [ "        }" ]
            | None -> if multi then [ "        {" ] @ indent core @ [ "        }" ] else core

        let branches core =
            layouts
            |> List.mapi (fun i l -> wrap (if i = lastIdx then None else guardCondition l.Range) (core (i = lastIdx) l))
            |> List.concat

        let lines = ResizeArray<string>()
        let add (s: string) = lines.Add s
        add "using McProtoNet.Protocol.Attributes;"
        add "using McProtoNet.Serialization;"
        add ""
        add "namespace McProtoNet.Protocol;"
        add ""
        add (supportAttrOf (layouts |> List.map (fun l -> l.Range)))
        let ps = apiFlags |> List.map (fun f -> sprintf "bool %s" (pascal f)) |> String.concat ", "
        add (sprintf "public readonly partial record struct %s(%s)" name ps)
        add "{"
        add (sprintf "    public static %s Read(ref MinecraftPrimitiveReader reader, int protocolVersion)" name)
        add "    {"
        add (sprintf "        ThrowHelper.ThrowIfProtocolNotSupported<%s>(protocolVersion);" name)
        for l in branches (fun _ l -> readCore l) do add l
        add "    }"
        add ""
        add "    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)"
        add "    {"
        add (sprintf "        ThrowHelper.ThrowIfProtocolNotSupported<%s>(protocolVersion);" name)
        for l in branches writeCore do add l
        add "    }"
        add "}"
        add ""
        String.Join("\n", lines)

    /// The C# code-generation target.
    let target : ILanguageTarget =
        { new ILanguageTarget with
            member _.Id = "csharp"
            member _.Extension = ".cs"
            member _.RenderType spec = render spec
            member _.RenderBitflags spec = renderBitflags spec }
