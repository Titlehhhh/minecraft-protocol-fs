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
            /// Reader / writer types and the parameter names used in generated signatures.
            ReaderType: string
            WriterType: string
            ReaderParam: string
            WriterParam: string
            VersionParam: string
            /// Version gate: attribute name, throw helper, open-range bound constants.
            SupportAttribute: string
            ThrowIfNotSupported: string
            StartProtocolConst: string
            LatestProtocolConst: string
            /// Generic read/write of a nested named type on the reader/writer.
            ReadNamedMethod: string
            WriteNamedMethod: string
            /// Names of the generated methods themselves.
            ReadMethodName: string
            WriteMethodName: string
            /// Interface every generated type implements (as `I<TSelf>`); None to skip.
            ProtocolInterface: string option
            /// Interface packets implement instead of `ProtocolInterface` (adds identity + ids).
            PacketInterface: string option
            /// Identity value type, phase/direction enums and the declarative packet attribute.
            IdentityType: string
            PhaseEnum: string
            DirectionEnum: string
            PacketAttributeName: string
            /// C# spellings of api leaf types that are not C# keywords.
            NbtType: string
            UuidType: string
            /// Wire primitive -> reader/writer methods.
            Primitives: Map<WireType, Primitive>
        }

    let private prim readCall writeMethod csType =
        {
            ReadCall = readCall
            WriteMethod = writeMethod
            CsType = csType
        }

    /// The McProtoNet-shaped default surface.
    let mcProtoNet: RuntimeSurface =
        {
            Namespace = "McProtoNet.Protocol"
            UsingAttributes = "McProtoNet.Protocol.Attributes"
            UsingSerialization = "McProtoNet.Serialization"
            UsingNbt = "McProtoNet.NBT"
            UsingSystem = "System"
            ReaderType = "MinecraftPrimitiveReader"
            WriterType = "MinecraftPrimitiveWriter"
            ReaderParam = "reader"
            WriterParam = "writer"
            VersionParam = "protocolVersion"
            SupportAttribute = "ProtocolSupport"
            ThrowIfNotSupported = "ThrowHelper.ThrowIfProtocolNotSupported"
            StartProtocolConst = "MinecraftVersion.StartProtocol"
            LatestProtocolConst = "MinecraftVersion.LatestProtocol"
            ReadNamedMethod = "ReadType"
            WriteNamedMethod = "WriteType"
            ReadMethodName = "Read"
            WriteMethodName = "Write"
            ProtocolInterface = Some "IProtocolType"
            PacketInterface = Some "IPacket"
            IdentityType = "PacketIdentity"
            PhaseEnum = "PacketPhase"
            DirectionEnum = "PacketDirection"
            PacketAttributeName = "Packet"
            NbtType = "NbtTag"
            UuidType = "Guid"
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
                        Nbt, prim "ReadNbtTag(false)!" "WriteNbt" "NbtTag"
                        AnonNbt, prim "ReadNbtTag(true)!" "WriteNbt" "NbtTag"
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
        | TNamed n -> n
        | TUnion n -> n

    /// Full read expression for a wire type, e.g. `reader.ReadVarInt()` or
    /// `reader.ReadType<Slot>(protocolVersion)`. `Error` carries the unsupported shape.
    let readExpr (s: RuntimeSurface) (w: WireType) : Result<string, string> =
        match w with
        | Named n -> Ok(sprintf "%s.%s<%s>(%s)" s.ReaderParam s.ReadNamedMethod n s.VersionParam)
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
        | _ ->
            match s.Primitives.TryFind w with
            | Some p -> Ok(sprintf "%s.%s(%s)" s.WriterParam p.WriteMethod v)
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
