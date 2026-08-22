namespace McProtocol.Codegen

open McProtocol.Dsl

/// The C# *runtime surface* the generated code is written against: every type name, method name
/// and parameter name the emitted source references, gathered into one replaceable record.
///
/// The default instance (`mcProtoNet`) mirrors McProtoNet's `MinecraftPrimitiveReader` /
/// `MinecraftPrimitiveWriter`. Retargeting the generator at a renamed reader, a different
/// namespace, or an entirely different runtime is a one-record change — the renderer in
/// `CSharp.fs` never hardcodes a runtime name.
module CSharpSurface =

    /// How one wire primitive maps onto the reader/writer surface: the full call on the reader
    /// (including any fixed arguments), the writer method name, and the C# type the read yields
    /// (also used for bitflags backing locals and narrowing casts on write).
    type Primitive =
        {
            ReadCall: string
            WriteMethod: string
            /// Fixed arguments appended after the value on the write call, leading comma included
            /// (""  for the usual one-argument writers). NBT needs it: the root-name flag must
            /// travel on the write side too, not only on the read side.
            WriteExtraArgs: string
            CsType: string
        }

    type RuntimeSurface =
        {
            /// Namespace the generated types live in.
            Namespace: string
            /// Usings: always-on (attributes, serialization) and conditional extras.
            UsingAttributes: string
            UsingSerialization: string
            UsingNbt: string
            UsingSystem: string
            /// Namespace of the discriminated-union source generator the union backend leans on.
            UsingUnion: string
            /// Reader / writer types and the parameter names used in generated signatures.
            ReaderType: string
            WriterType: string
            ReaderParam: string
            WriterParam: string
            VersionParam: string
            /// Union surface: the marker attribute the generator reads, the extra read parameter
            /// carrying the discriminator the container already consumed, and the method that
            /// gives that value back on the write side.
            UnionAttribute: string
            DiscriminatorParam: string
            DiscriminatorMethodName: string
            /// Version gate: attribute name, throw helper, open-range bound constants.
            SupportAttribute: string
            ThrowIfNotSupported: string
            StartProtocolConst: string
            LatestProtocolConst: string
            /// Generic read/write of a nested named type on the reader/writer.
            ReadNamedMethod: string
            WriteNamedMethod: string
            /// Exactly-n bytes, no length prefix (`FixedBytes n`). The count is part of the wire
            /// type, not of the stream, so it cannot live in the `Primitives` map: the read takes
            /// it as an argument and the write takes it as the length the value must have.
            ReadFixedBytesMethod: string
            WriteFixedBytesMethod: string
            /// Names of the generated methods themselves.
            ReadMethodName: string
            WriteMethodName: string
            /// Interface every generated type implements (as `I<TSelf>`); None to skip.
            ProtocolInterface: string option
            /// Interface packets implement instead of `ProtocolInterface` (adds identity + ids).
            PacketInterface: string option
            /// Non-generic interface every packet implements *in addition* to `PacketInterface`,
            /// so a decoded packet held by a plain reference still has a common type. Carries one
            /// instance member, emitted as an explicit implementation over the static identity:
            /// `PacketIdentity IPacket.Identity => Identity;`. None to skip.
            PacketBaseInterface: string option
            /// Identity value type, phase/direction enums and the declarative packet attribute.
            IdentityType: string
            PhaseEnum: string
            DirectionEnum: string
            /// The members of `PhaseEnum` / `DirectionEnum` in the numeric order the runtime
            /// declares them. The registry's flat lookup indexes its tables by `(int)phase` and
            /// `(int)direction`, so the slot of a (phase, direction) is decided here and not by
            /// the order the renderer happens to walk in. Reordering the runtime enum without
            /// reordering these lists mismaps every id: `PacketRegistryTests` is the guard.
            PhaseOrder: ProtocolState list
            DirectionOrder: Direction list
            PacketAttributeName: string
            PacketFieldAttributeName: string
            WrongLayerExceptionType: string
            /// C# spellings of api leaf types that are not C# keywords.
            NbtType: string
            UuidType: string
            /// Generic runtime type behind `RegistryHolder`: a struct carrying either a registry
            /// id or an inline payload. It implements the same protocol-type interface as any
            /// nested named type, so it travels on the existing ReadNamed/WriteNamed pair.
            HolderType: string
            /// Wire primitive -> reader/writer methods.
            Primitives: Map<WireType, Primitive>
        }

    let private prim readCall writeMethod csType =
        {
            ReadCall = readCall
            WriteMethod = writeMethod
            WriteExtraArgs = ""
            CsType = csType
        }

    /// Same as `prim`, for a writer call that carries fixed arguments after the value.
    let private primWith readCall writeMethod writeExtraArgs csType =
        {
            ReadCall = readCall
            WriteMethod = writeMethod
            WriteExtraArgs = writeExtraArgs
            CsType = csType
        }

    /// The McProtoNet-shaped default surface.
    let mcProtoNet: RuntimeSurface =
        {
            Namespace = "McProtoNet.Protocol"
            UsingAttributes = "McProtoNet.Protocol.Attributes"
            UsingSerialization = "McProtoNet.Primitives"
            UsingNbt = "McProtoNet.NBT"
            UsingSystem = "System"
            UsingUnion = "Dunet"
            ReaderType = "MinecraftPrimitiveReader"
            WriterType = "MinecraftPrimitiveWriter"
            ReaderParam = "reader"
            WriterParam = "writer"
            VersionParam = "protocolVersion"
            UnionAttribute = "Union"
            DiscriminatorParam = "discriminator"
            DiscriminatorMethodName = "Discriminator"
            SupportAttribute = "ProtocolSupport"
            ThrowIfNotSupported = "ThrowHelper.ThrowIfProtocolNotSupported"
            StartProtocolConst = "MinecraftVersion.StartProtocol"
            LatestProtocolConst = "MinecraftVersion.LatestProtocol"
            ReadNamedMethod = "ReadType"
            WriteNamedMethod = "WriteType"
            ReadFixedBytesMethod = "ReadFixedBytes"
            WriteFixedBytesMethod = "WriteFixedBytes"
            ReadMethodName = "Read"
            WriteMethodName = "Write"
            ProtocolInterface = Some "IProtocolType"
            PacketInterface = Some "IPacket"
            PacketBaseInterface = Some "IPacket"
            IdentityType = "PacketIdentity"
            PhaseEnum = "PacketPhase"
            DirectionEnum = "PacketDirection"
            PhaseOrder = [ Handshaking; Status; Login; Configuration; Play ]
            DirectionOrder = [ Clientbound; Serverbound ]
            PacketAttributeName = "Packet"
            PacketFieldAttributeName = "PacketField"
            WrongLayerExceptionType = "WrongLayerException"
            NbtType = "NbtTag"
            UuidType = "Guid"
            HolderType = "RegistryOrInline"
            Primitives =
                Map.ofList
                    [
                        Bool, prim "ReadBoolean()" "WriteBoolean" "bool"
                        VarInt, prim "ReadVarInt()" "WriteVarInt" "int"
                        VarLong, prim "ReadVarLong()" "WriteVarLong" "long"
                        I8, prim "ReadSignedByte()" "WriteSignedByte" "sbyte"
                        U8, prim "ReadUnsignedByte()" "WriteUnsignedByte" "byte"
                        I16, prim "ReadSignedShort()" "WriteSignedShort" "short"
                        U16, prim "ReadUnsignedShort()" "WriteUnsignedShort" "ushort"
                        I32, prim "ReadSignedInt()" "WriteSignedInt" "int"
                        U32, prim "ReadUnsignedInt()" "WriteUnsignedInt" "uint"
                        I64, prim "ReadSignedLong()" "WriteSignedLong" "long"
                        U64, prim "ReadUnsignedLong()" "WriteUnsignedLong" "ulong"
                        F32, prim "ReadFloat()" "WriteFloat" "float"
                        F64, prim "ReadDouble()" "WriteDouble" "double"
                        Str, prim "ReadString()" "WriteString" "string"
                        Uuid, prim "ReadUUID()" "WriteUUID" "Guid"
                        // The runtime flag means "the root tag carries a name": true for the
                        // classic named root, false for the nameless network root. Both sides
                        // must agree, or a round-trip silently shifts by the name field.
                        Nbt, primWith "ReadNbtTag(true)!" "WriteNbt" ", true" "NbtTag"
                        AnonNbt, prim "ReadNbtTag(false)!" "WriteNbt" "NbtTag"
                        ByteArray, prim "ReadByteArray()" "WriteByteArray" "byte[]"
                        RestBytes, prim "ReadRestBytes()" "WriteRestBytes" "byte[]"
                    ]
        }

    // ----- lookups the renderer leans on -----

    /// The api-model C# type of an api type, spelled with this surface's names.
    let rec csType (s: RuntimeSurface) (t: ApiType) : string =
        match t with
        | TBool -> "bool"
        | TInt -> "int"
        | TLong -> "long"
        | TFloat -> "float"
        | TDouble -> "double"
        | TString -> "string"
        | TUuid -> s.UuidType
        | TNbt -> s.NbtType
        | TBytes -> "byte[]"
        | TArray inner -> csType s inner + "[]"
        | TOption inner -> csType s inner + "?"
        | THolder inner -> sprintf "%s<%s>" s.HolderType (csType s inner)
        | TNamed n -> n
        | TUnion n -> n
        | TEnum n -> n

    /// The C# type a wire type produces when it stands on its own — what a holder needs to name
    /// as its generic argument. `None` means the shape has no single C# spelling.
    let wireLeafCsType (s: RuntimeSurface) (w: WireType) : string option =
        match w with
        | Named n -> Some n
        | EnumRef(n, _) -> Some n
        | FixedBytes _ -> Some "byte[]"
        | _ -> s.Primitives.TryFind w |> Option.map (fun p -> p.CsType)

    /// `RegistryOrInline<Payload>` — the C# type a `RegistryHolder` wire field reads and writes.
    let holderCsType (s: RuntimeSurface) (inner: WireType) : string option =
        wireLeafCsType s inner |> Option.map (sprintf "%s<%s>" s.HolderType)

    /// The primitive an enum table may travel as: integral and narrow enough that its value fits
    /// an `int` without loss, because the generated enum carries exactly one `int Value`.
    let enumBacking (s: RuntimeSurface) (w: WireType) : Primitive option =
        match w with
        | I8
        | U8
        | I16
        | U16
        | I32
        | VarInt -> s.Primitives.TryFind w
        | _ -> None

    /// Full read expression for a wire type, e.g. `reader.ReadVarInt()` or
    /// `reader.ReadType<Slot>(protocolVersion)`. `Error` carries the unsupported shape.
    let readExpr (s: RuntimeSurface) (w: WireType) : Result<string, string> =
        match w with
        | Named n -> Ok(sprintf "%s.%s<%s>(%s)" s.ReaderParam s.ReadNamedMethod n s.VersionParam)
        | EnumRef(n, None) -> Ok(sprintf "%s.%s<%s>(%s)" s.ReaderParam s.ReadNamedMethod n s.VersionParam)
        | EnumRef(n, Some b) ->
            match enumBacking s b with
            | Some p -> Ok(sprintf "new %s((int)%s.%s)" n s.ReaderParam p.ReadCall)
            | None -> Error(sprintf "%A" w)
        | RegistryHolder inner ->
            match holderCsType s inner with
            | Some t -> Ok(sprintf "%s.%s<%s>(%s)" s.ReaderParam s.ReadNamedMethod t s.VersionParam)
            | None -> Error(sprintf "%A" w)
        | FixedBytes n -> Ok(sprintf "%s.%s(%d)" s.ReaderParam s.ReadFixedBytesMethod n)
        | _ ->
            match s.Primitives.TryFind w with
            | Some p -> Ok(sprintf "%s.%s" s.ReaderParam p.ReadCall)
            | None -> Error(sprintf "%A" w)

    /// Full write expression for a wire type. `cast` (usually the primitive's own C# type) is
    /// inserted when the api-side value is wider than the wire primitive (e.g. int api, i8 wire).
    let writeExpr (s: RuntimeSurface) (w: WireType) (cast: string option) (value: string) : Result<string, string> =
        let v =
            match cast with
            | Some c -> sprintf "(%s)%s" c value
            | None -> value

        match w with
        | Named n -> Ok(sprintf "%s.%s<%s>(%s, %s)" s.WriterParam s.WriteNamedMethod n v s.VersionParam)
        | EnumRef(n, None) -> Ok(sprintf "%s.%s<%s>(%s, %s)" s.WriterParam s.WriteNamedMethod n v s.VersionParam)
        | EnumRef(_, Some b) ->
            match enumBacking s b with
            | Some p -> Ok(sprintf "%s.%s((%s)%s.Value)" s.WriterParam p.WriteMethod p.CsType value)
            | None -> Error(sprintf "%A" w)
        | RegistryHolder inner ->
            match holderCsType s inner with
            | Some t -> Ok(sprintf "%s.%s<%s>(%s, %s)" s.WriterParam s.WriteNamedMethod t v s.VersionParam)
            | None -> Error(sprintf "%A" w)
        | FixedBytes n -> Ok(sprintf "%s.%s(%s, %d)" s.WriterParam s.WriteFixedBytesMethod v n)
        | _ ->
            match s.Primitives.TryFind w with
            | Some p -> Ok(sprintf "%s.%s(%s%s)" s.WriterParam p.WriteMethod v p.WriteExtraArgs)
            | None -> Error(sprintf "%A" w)

    /// The primitive for a bitflags backing integer — integral wire types only.
    let integralBacking (s: RuntimeSurface) (w: WireType) : Primitive option =
        match w with
        | U8
        | U16
        | U32
        | U64
        | I8
        | I16
        | I32
        | I64
        | VarInt
        | VarLong -> s.Primitives.TryFind w
        | _ -> None
