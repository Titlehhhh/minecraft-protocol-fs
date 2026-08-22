namespace McProtocol.Dsl

/// Core algebra of the protocol DSL: the wire/model AST shared by every spec.
/// Contains no concrete protocol content — only the shapes specs are built from.
[<AutoOpen>]
module Ast =

    type Ver = int

    type VersionRange =
        | All
        | Since of Ver
        | Until of Ver
        | Between of Ver * Ver

    type WireType =
        | Void

        // numbers / primitives
        | VarInt
        | VarLong
        | I8
        | U8
        | I16
        | U16
        | I32
        | U32
        | I64
        | U64
        | F32
        | F64
        | Bool

        // minecraft primitives
        | Str
        | Uuid
        | Nbt
        | AnonNbt
        | ByteArray
        | FixedBytes of int
        // remaining bytes of the packet, no length prefix (protodef restBuffer)
        | RestBytes

        // wrappers
        | Array of WireType * ArrayCount
        | Option of WireType

        // protodef registryEntryHolder: a varint where 0 means the inline payload follows and
        // n > 0 means registry entry n - 1. A wrapper, not a type of its own — the api side is
        // a holder over the payload type, never a class wrapping one field.
        | RegistryHolder of WireType

        // read items until sentinel byte/value
        // entityMetadata = SentinelArray(Named "EntityMetadataEntry", 255)
        | SentinelArray of item: WireType * endValue: int

        // low-level fallback
        | Switch of discriminator: string * SwitchCase list

        // named complex type
        | Named of string

        // reference to a named `enumType`. `Backing = None` reads it through its own canonical
        // wire (the backing its layouts declare); `Some w` reads the raw integer as `w` at this
        // site instead, for tables the protocol carries under two different integer widths
        // (Gamemode: i8 inside SpawnInfo, varint in change_gamemode).
        | EnumRef of name: string * backing: WireType option

    and ArrayCount =
        | VarIntCount
        | FixedCount of int
        // length prefix encoded as an arbitrary integer wire type (e.g. i32 in old packets)
        | TypedCount of WireType

    and SwitchCase = { On: SwitchKey list; Type: WireType }

    and SwitchKey =
        | IntKey of int
        | StrKey of string
        | Default


    type WireEntry =
        | Read of wire: string * WireType * api: string
        | Discard of wire: string * WireType

        // block is read only if field != 0
        | IfNonZero of field: string * WireEntry list

        // independent optional field by discriminator
        | ReadOpt of wire: string * WireType * api: string * disc: string * keys: int list

        // named nested API object
        | ReadBlock of typ: WireType * api: string * entries: WireEntry list

        // top-level named union
        | ReadUnion of disc: string * unionName: string * api: string

        // fallback for small local unions
        | InlineUnion of disc: string * UnionArm list

    and UnionArm =
        {
            Keys: int list
            Name: string
            Entries: WireEntry list
        }


    type ApiType =
        | TBool
        | TInt
        | TLong
        | TFloat
        | TDouble
        | TString
        | TUuid
        | TNbt
        | TBytes
        | TArray of ApiType
        | TOption of ApiType
        | THolder of ApiType
        | TNamed of string
        | TUnion of string
        | TEnum of string

    type ApiField =
        {
            Name: string
            Type: ApiType
            Present: VersionRange
        }

    type WireLayout =
        {
            Range: VersionRange
            Entries: WireEntry list
        }

    type NamedTypeSpec =
        {
            Name: string
            ApiFields: ApiField list
            Layouts: WireLayout list
        }

    type UnionLayout =
        {
            Range: VersionRange
            Arms: UnionArm list
        }

    type UnionTypeSpec =
        {
            Name: string
            Layouts: UnionLayout list
        }

    // A backing integer whose named bits map to boolean api fields. `Flags` are the wire flag
    // names in bit order (bit i = 1 <<< i); the api is the union of all layouts' flags as bools.
    type BitflagsLayout =
        {
            Range: VersionRange
            Backing: WireType
            Flags: string list
        }

    type BitflagsSpec =
        {
            Name: string
            Layouts: BitflagsLayout list
        }

    // A closed integer table whose ids carry names (protodef `mapper`). The generated type is a
    // record struct over the raw int, never a C# enum: an id the table does not know must survive
    // a round-trip unchanged. `Values` are (id, wire name) pairs; `Backing` is the integer the
    // table travels as in that version range.
    type EnumLayout =
        {
            Range: VersionRange
            Backing: WireType
            Values: (int * string) list
        }

    type EnumSpec =
        {
            Name: string
            Layouts: EnumLayout list
        }

    type ProtocolState =
        | Handshaking
        | Status
        | Login
        | Play
        | Configuration

    type Direction =
        | Clientbound
        | Serverbound

    type PacketSpec =
        {
            ClassName: string
            State: ProtocolState
            Direction: Direction
            Since: VersionRange
            ApiFields: ApiField list
            Layouts: WireLayout list
            ProtoName: string option
            Ids: (int * int * int) list
        }

    type ProtocolSpec =
        {
            Types: NamedTypeSpec list
            Unions: UnionTypeSpec list
            Bitflags: BitflagsSpec list
            Enums: EnumSpec list
            Packets: PacketSpec list
        }
