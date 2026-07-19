
// ===== ТИПЫ =====

type Ver = int

type VersionRange =
    | All
    | Since   of Ver
    | Until   of Ver
    | Between of Ver * Ver

type WireType =
    | Void

    // numbers / primitives
    | VarInt
    | VarLong
    | I8 | U8
    | I16 | U16
    | I32 | U32
    | I64 | U64
    | F32 | F64
    | Bool

    // minecraft primitives
    | Str
    | Uuid
    | Nbt
    | AnonNbt
    | ByteArray
    | FixedBytes of int

    // wrappers
    | Array of WireType * ArrayCount
    | Option of WireType

    // read items until sentinel byte/value
    // entityMetadata = SentinelArray(Named "EntityMetadataEntry", 255)
    | SentinelArray of item: WireType * endValue: int

    // low-level fallback
    | Switch of discriminator: string * SwitchCase list

    // named complex type
    | Named of string

and ArrayCount =
    | VarIntCount
    | FixedCount of int

and SwitchCase = {
    On   : SwitchKey list
    Type : WireType
}

and SwitchKey =
    | IntKey of int
    | StrKey of string
    | Default


type WireEntry =
    | Read    of wire: string * WireType * api: string
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

and UnionArm = {
    Keys    : int list
    Name    : string
    Entries : WireEntry list
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
    | TArray  of ApiType
    | TOption of ApiType
    | TNamed  of string
    | TUnion  of string

type ApiField = {
    Name    : string
    Type    : ApiType
    Present : VersionRange
}

type WireLayout = {
    Range   : VersionRange
    Entries : WireEntry list
}

type NamedTypeSpec = {
    Name      : string
    ApiFields : ApiField list
    Layouts   : WireLayout list
}

type UnionLayout = {
    Range : VersionRange
    Arms  : UnionArm list
}

type UnionTypeSpec = {
    Name    : string
    Layouts : UnionLayout list
}

type ProtocolState =
    | Login
    | Play
    | Configuration

type Direction =
    | Clientbound
    | Serverbound

type PacketSpec = {
    ClassName : string
    State     : ProtocolState
    Direction : Direction
    Since     : VersionRange
    ApiFields : ApiField list
    Layouts   : WireLayout list
}

type ProtocolSpec = {
    Types   : NamedTypeSpec list
    Unions  : UnionTypeSpec list
    Packets : PacketSpec list
}


// ===== BUILDERS =====

type PacketBuilder(name, state, direction, since) =
    let mutable apiFields : ApiField list = []
    let mutable layouts   : WireLayout list = []

    member _.Yield(()) = ()
    member _.Zero() = ()
    member _.Delay(f) = f()
    member _.Combine((), f) = f()

    [<CustomOperation("api")>]
    member _.Api((), fields) =
        apiFields <- fields

    [<CustomOperation("wire")>]
    member _.Wire((), range, entries) =
        layouts <- layouts @ [{ Range = range; Entries = entries }]

    member _.Run(()) = {
        ClassName = name
        State     = state
        Direction = direction
        Since     = since
        ApiFields = apiFields
        Layouts   = layouts
    }

type NamedTypeBuilder(name) =
    let mutable apiFields : ApiField list = []
    let mutable layouts   : WireLayout list = []

    member _.Yield(()) = ()
    member _.Zero() = ()
    member _.Delay(f) = f()
    member _.Combine((), f) = f()

    [<CustomOperation("api")>]
    member _.Api((), fields) =
        apiFields <- fields

    [<CustomOperation("wire")>]
    member _.Wire((), range, entries) =
        layouts <- layouts @ [{ Range = range; Entries = entries }]

    member _.Run(()) = {
        Name      = name
        ApiFields = apiFields
        Layouts   = layouts
    }

type UnionTypeBuilder(name) =
    let mutable layouts : UnionLayout list = []

    member _.Yield(()) = ()
    member _.Zero() = ()
    member _.Delay(f) = f()
    member _.Combine((), f) = f()

    [<CustomOperation("cases")>]
    member _.Cases((), range, arms) =
        layouts <- layouts @ [{ Range = range; Arms = arms }]

    member _.Run(()) = {
        Name    = name
        Layouts = layouts
    }


// ===== CONSTRUCTORS =====

let packet name state direction since =
    PacketBuilder(name, state, direction, since)

let namedType name =
    NamedTypeBuilder(name)

let unionType name =
    UnionTypeBuilder(name)


// ===== HELPERS — API =====

let field name t present : ApiField =
    { Name = name; Type = t; Present = present }


// ===== HELPERS — WIRE =====

let read wire t api =
    Read(wire, t, api)

let discard wire t =
    Discard(wire, t)

let ifNonZero field entries =
    IfNonZero(field, entries)

let readOpt wire t api disc keys =
    ReadOpt(wire, t, api, disc, keys)

let readBlock typ api entries =
    ReadBlock(typ, api, entries)

let readUnion disc unionName api =
    ReadUnion(disc, unionName, api)

let inlineUnion disc arms =
    InlineUnion(disc, arms)

let arm keys name entries =
    { Keys = keys; Name = name; Entries = entries }

let case keys name entries =
    arm keys name entries

let case1 key name entries =
    arm [key] name entries


// ===== COMMON WIRE ALIASES =====

let entityMetadataWire =
    SentinelArray(Named "EntityMetadataEntry", 255)


// ===== ИМЕНОВАННЫЕ ТИПЫ =====

let mapColorData =
    namedType "MapColorData" {
        api [
            field "Rows" TInt All
            field "X"    TInt All
            field "Y"    TInt All
            field "Data" TBytes All
        ]

        wire All [
            read "rows" I8        "Rows"
            read "x"    I8        "X"
            read "y"    I8        "Y"
            read "data" ByteArray "Data"
        ]
    }

let rotations =
    namedType "Rotations" {
        api [
            field "Pitch" TFloat All
            field "Yaw"   TFloat All
            field "Roll"  TFloat All
        ]

        wire All [
            read "pitch" F32 "Pitch"
            read "yaw"   F32 "Yaw"
            read "roll"  F32 "Roll"
        ]
    }

let villagerData =
    namedType "VillagerData" {
        api [
            field "Type"       TInt All
            field "Profession" TInt All
            field "Level"      TInt All
        ]

        wire All [
            read "villagerType"       VarInt "Type"
            read "villagerProfession" VarInt "Profession"
            read "level"              VarInt "Level"
        ]
    }

let entityMetadataEntry =
    namedType "EntityMetadataEntry" {
        api [
            field "Index" TInt All
            field "Value" (TUnion "EntityMetadataValue") All
        ]

        wire (Since 766) [
            read      "key"  U8     "Index"
            read      "type" VarInt "_type"
            readUnion "_type" "EntityMetadataValue" "Value"
        ]
    }


// ===== UNION TYPES =====

let teamAction =
    unionType "TeamAction" {
        cases (Until 764) [
            case1 0 "Created" [
                read "name"          Str    "Name"
                read "friendlyFire"  I8     "FriendlyFire"
                read "nameTagVis"    Str    "NameTagVisibility"
                read "collisionRule" Str    "CollisionRule"
                read "formatting"    VarInt "Formatting"
                read "prefix"        Str    "Prefix"
                read "suffix"        Str    "Suffix"
                read "players"       (Array(Str, VarIntCount)) "Players"
            ]

            case1 1 "Removed" []

            case1 2 "Updated" [
                read "name"          Str    "Name"
                read "friendlyFire"  I8     "FriendlyFire"
                read "nameTagVis"    Str    "NameTagVisibility"
                read "collisionRule" Str    "CollisionRule"
                read "formatting"    VarInt "Formatting"
                read "prefix"        Str    "Prefix"
                read "suffix"        Str    "Suffix"
            ]

            case1 3 "PlayersAdded" [
                read "players" (Array(Str, VarIntCount)) "Players"
            ]

            case1 4 "PlayersRemoved" [
                read "players" (Array(Str, VarIntCount)) "Players"
            ]
        ]

        cases (Since 771) [
            case1 0 "Created" [
                read    "name"          AnonNbt "Name"
                discard "flags"         U8
                read    "nameTagVis"    VarInt  "NameTagVisibility"
                read    "collisionRule" VarInt  "CollisionRule"
                read    "formatting"    VarInt  "Formatting"
                read    "prefix"        AnonNbt "Prefix"
                read    "suffix"        AnonNbt "Suffix"
                read    "players"       (Array(Str, VarIntCount)) "Players"
            ]

            case1 1 "Removed" []

            case1 2 "Updated" [
                read    "name"          AnonNbt "Name"
                discard "flags"         U8
                read    "nameTagVis"    VarInt  "NameTagVisibility"
                read    "collisionRule" VarInt  "CollisionRule"
                read    "formatting"    VarInt  "Formatting"
                read    "prefix"        AnonNbt "Prefix"
                read    "suffix"        AnonNbt "Suffix"
            ]

            case [3; 4] "PlayersChanged" [
                read "players" (Array(Str, VarIntCount)) "Players"
            ]
        ]
    }

let entityMetadataValue =
    unionType "EntityMetadataValue" {
        cases (Between(766, 769)) [
            case1 0  "Byte"                [ read "value" I8 "Value" ]
            case1 1  "Int"                 [ read "value" VarInt "Value" ]
            case1 2  "Long"                [ read "value" VarLong "Value" ]
            case1 3  "Float"               [ read "value" F32 "Value" ]
            case1 4  "String"              [ read "value" Str "Value" ]
            case1 5  "Component"           [ read "value" AnonNbt "Value" ]
            case1 6  "OptionalComponent"   [ read "value" (Option AnonNbt) "Value" ]
            case1 7  "ItemStack"           [ read "value" (Named "Slot") "Value" ]
            case1 8  "Boolean"             [ read "value" Bool "Value" ]
            case1 9  "Rotations"           [ read "value" (Named "Rotations") "Value" ]
            case1 10 "BlockPos"            [ read "value" (Named "Position") "Value" ]
            case1 11 "OptionalBlockPos"    [ read "value" (Option(Named "Position")) "Value" ]
            case1 12 "Direction"           [ read "value" VarInt "Value" ]
            case1 13 "OptionalUuid"        [ read "value" (Option Uuid) "Value" ]
            case1 14 "BlockState"          [ read "value" VarInt "Value" ]
            case1 15 "OptionalBlockState"  [ read "value" VarInt "Value" ]
            case1 16 "CompoundTag"         [ read "value" AnonNbt "Value" ]
            case1 17 "Particle"            [ read "value" (Named "Particle") "Value" ]
            case1 18 "Particles"           [ read "value" (Array(Named "Particle", VarIntCount)) "Value" ]
            case1 19 "VillagerData"        [ read "value" (Named "VillagerData") "Value" ]
            case1 20 "OptionalUnsignedInt" [ read "value" VarInt "Value" ]
            case1 21 "Pose"                [ read "value" VarInt "Value" ]
            case1 22 "CatVariant"          [ read "value" VarInt "Value" ]
            case1 23 "WolfVariant"         [ read "value" (Named "RegistryWolfVariant") "Value" ]
            case1 24 "FrogVariant"         [ read "value" VarInt "Value" ]
            case1 25 "OptionalGlobalPos"   [ read "value" (Option Str) "Value" ]
            case1 26 "PaintingVariant"     [ read "value" (Named "RegistryPaintingVariant") "Value" ]
            case1 27 "SnifferState"        [ read "value" VarInt "Value" ]
            case1 28 "ArmadilloState"      [ read "value" VarInt "Value" ]
            case1 29 "Vector3"             [ read "value" (Named "Vec3f") "Value" ]
            case1 30 "Quaternion"          [ read "value" (Named "Vec4f") "Value" ]
        ]

        cases (Since 770) [
            case1 0  "Byte"                [ read "value" I8 "Value" ]
            case1 1  "Int"                 [ read "value" VarInt "Value" ]
            case1 2  "Long"                [ read "value" VarLong "Value" ]
            case1 3  "Float"               [ read "value" F32 "Value" ]
            case1 4  "String"              [ read "value" Str "Value" ]
            case1 5  "Component"           [ read "value" AnonNbt "Value" ]
            case1 6  "OptionalComponent"   [ read "value" (Option AnonNbt) "Value" ]
            case1 7  "ItemStack"           [ read "value" (Named "Slot") "Value" ]
            case1 8  "Boolean"             [ read "value" Bool "Value" ]
            case1 9  "Rotations"           [ read "value" (Named "Rotations") "Value" ]
            case1 10 "BlockPos"            [ read "value" (Named "Position") "Value" ]
            case1 11 "OptionalBlockPos"    [ read "value" (Option(Named "Position")) "Value" ]
            case1 12 "Direction"           [ read "value" VarInt "Value" ]
            case1 13 "OptionalUuid"        [ read "value" (Option Uuid) "Value" ]
            case1 14 "BlockState"          [ read "value" VarInt "Value" ]
            case1 15 "OptionalBlockState"  [ read "value" VarInt "Value" ]
            case1 16 "CompoundTag"         [ read "value" AnonNbt "Value" ]
            case1 17 "Particle"            [ read "value" (Named "Particle") "Value" ]
            case1 18 "Particles"           [ read "value" (Array(Named "Particle", VarIntCount)) "Value" ]
            case1 19 "VillagerData"        [ read "value" (Named "VillagerData") "Value" ]
            case1 20 "OptionalUnsignedInt" [ read "value" VarInt "Value" ]
            case1 21 "Pose"                [ read "value" VarInt "Value" ]
            case1 22 "CatVariant"          [ read "value" VarInt "Value" ]
            case1 23 "CowVariant"          [ read "value" VarInt "Value" ]
            case1 24 "WolfVariant"         [ read "value" VarInt "Value" ]
            case1 25 "WolfSoundVariant"    [ read "value" VarInt "Value" ]
            case1 26 "FrogVariant"         [ read "value" VarInt "Value" ]
            case1 27 "PigVariant"          [ read "value" VarInt "Value" ]
            case1 28 "ChickenVariant"      [ read "value" (Named "RegistryChickenVariant") "Value" ]
            case1 29 "OptionalGlobalPos"   [ read "value" (Option Str) "Value" ]
            case1 30 "PaintingVariant"     [ read "value" (Named "RegistryPaintingVariant") "Value" ]
            case1 31 "SnifferState"        [ read "value" VarInt "Value" ]
            case1 32 "ArmadilloState"      [ read "value" VarInt "Value" ]
            case1 33 "Vector3"             [ read "value" (Named "Vec3f") "Value" ]
            case1 34 "Quaternion"          [ read "value" (Named "Vec4f") "Value" ]
        ]
    }


// ===== ПАКЕТЫ =====

let unloadChunk =
    packet "UnloadChunkPacket" Play Clientbound All {
        api [
            field "ChunkX" TInt All
            field "ChunkZ" TInt All
        ]

        wire (Until 763) [
            read "x" I32 "ChunkX"
            read "z" I32 "ChunkZ"
        ]

        wire (Since 764) [
            read "z" I32 "ChunkZ"
            read "x" I32 "ChunkX"
        ]
    }

let mapPacket =
    packet "MapPacket" Play Clientbound All {
        api [
            field "ItemDamage" TInt All
            field "Scale"      TInt All
            field "Locked"     TBool All
            field "Columns"    TInt All
            field "ColorData"  (TOption(TNamed "MapColorData")) All
        ]

        wire (Until 754) [
            read    "itemDamage"       VarInt "ItemDamage"
            read    "scale"            I8     "Scale"
            discard "trackingPosition" Bool
            read    "locked"           Bool   "Locked"
            discard "icons"            (Array(Named "MapIcon", VarIntCount))
            read    "columns"          I8     "Columns"

            ifNonZero "columns" [
                readBlock (Named "MapColorData") "ColorData" [
                    read "rows" I8        "Rows"
                    read "x"    I8        "X"
                    read "y"    I8        "Y"
                    read "data" ByteArray "Data"
                ]
            ]
        ]

        wire (Since 765) [
            read    "itemDamage" VarInt "ItemDamage"
            read    "scale"      I8     "Scale"
            read    "locked"     Bool   "Locked"
            discard "icons"      (Option(Array(Named "MapIcon", VarIntCount)))
            read    "columns"    U8     "Columns"

            ifNonZero "columns" [
                readBlock (Named "MapColorData") "ColorData" [
                    read "rows" I8        "Rows"
                    read "x"    I8        "X"
                    read "y"    I8        "Y"
                    read "data" ByteArray "Data"
                ]
            ]
        ]
    }

let teamsPacket =
    packet "TeamsPacket" Play Clientbound All {
        api [
            field "TeamName" TString All
            field "Action"   (TUnion "TeamAction") All
        ]

        wire (Until 764) [
            read      "team" Str "TeamName"
            read      "mode" I8  "_mode"
            readUnion "_mode" "TeamAction" "Action"
        ]

        wire (Since 771) [
            read      "team" Str    "TeamName"
            read      "mode" VarInt "_mode"
            readUnion "_mode" "TeamAction" "Action"
        ]
    }

let entityMetadataPacket =
    packet "EntityMetadataPacket" Play Clientbound All {
        api [
            field "EntityId" TInt All
            field "Metadata" (TArray(TNamed "EntityMetadataEntry")) All
        ]

        wire All [
            read "entityId" VarInt "EntityId"
            read "metadata" entityMetadataWire "Metadata"
        ]
    }

let windowClick =
    packet "WindowClickPacket" Play Serverbound All {
        api [
            field "WindowId"     TInt All
            field "StateId"      TInt (Since 756)
            field "Slot"         TInt All
            field "MouseButton"  TInt All
            field "Mode"         TInt All
            field "CursorItem"   (TNamed "Slot") All
        ]

        wire (Until 754) [
            read    "windowId"    U8   "WindowId"
            read    "slot"        I16  "Slot"
            read    "mouseButton" I8   "MouseButton"
            discard "action"      I16
            read    "mode"        I8   "Mode"
            discard "item"        (Named "Slot")
            read    "cursorItem"  (Named "Slot") "CursorItem"
        ]

        wire (Between(755, 755)) [
            read    "windowId"     U8  "WindowId"
            read    "slot"         I16 "Slot"
            read    "mouseButton"  I8  "MouseButton"
            read    "mode"         I8  "Mode"
            discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
            read    "cursorItem"   (Named "Slot") "CursorItem"
        ]

        wire (Between(756, 765)) [
            read    "windowId"     U8     "WindowId"
            read    "stateId"      VarInt "StateId"
            read    "slot"         I16    "Slot"
            read    "mouseButton"  I8     "MouseButton"
            read    "mode"         VarInt "Mode"
            discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
            read    "cursorItem"   (Named "Slot") "CursorItem"
        ]

        wire (Since 770) [
            read    "windowId"     U8     "WindowId"
            read    "stateId"      VarInt "StateId"
            read    "slot"         I16    "Slot"
            read    "mouseButton"  I8     "MouseButton"
            read    "mode"         VarInt "Mode"
            discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
            read    "cursorItem"   (Option(Named "HashedSlot")) "CursorItem"
        ]
    }


// ===== ПРОТОКОЛ =====

let protocol =
    {
        Types = [
            mapColorData
            rotations
            villagerData
            entityMetadataEntry
        ]

        Unions = [
            teamAction
            entityMetadataValue
        ]

        Packets = [
            unloadChunk
            mapPacket
            teamsPacket
            entityMetadataPacket
            windowClick
        ]
    }


// ===== ПРИНТ =====

let printField indent (f: ApiField) =
    let pad = String.replicate indent "  "
    printfn "%s%-20s : %-24A [%A]" pad f.Name f.Type f.Present

let rec printEntry indent (entry: WireEntry) =
    let pad = String.replicate indent "  "

    match entry with
    | Read(wire, t, api) ->
        printfn "%sread %-20s -> %-20s [%A]" pad wire api t

    | Discard(wire, t) ->
        printfn "%sdiscard %-17s [%A]" pad wire t

    | IfNonZero(field, entries) ->
        printfn "%sif %s != 0:" pad field
        for e in entries do
            printEntry (indent + 1) e

    | ReadOpt(wire, t, api, disc, keys) ->
        printfn "%sreadOpt %-17s -> %-20s when %s in %A [%A]" pad wire api disc keys t

    | ReadBlock(typ, api, entries) ->
        printfn "%sreadBlock -> %-20s [%A]" pad api typ
        for e in entries do
            printEntry (indent + 1) e

    | ReadUnion(disc, unionName, api) ->
        printfn "%sreadUnion %s -> %s : %s" pad disc api unionName

    | InlineUnion(disc, arms) ->
        printfn "%sinlineUnion(%s):" pad disc
        for a in arms do
            printfn "%s  [%A] -> %s" pad a.Keys a.Name
            for e in a.Entries do
                printEntry (indent + 2) e

let printNamedType (spec: NamedTypeSpec) =
    printfn "=== type %s ===" spec.Name

    printfn "API:"
    for f in spec.ApiFields do
        printField 1 f

    for l in spec.Layouts do
        printfn "Wire [%A]:" l.Range
        for e in l.Entries do
            printEntry 1 e

    printfn ""

let printUnion (spec: UnionTypeSpec) =
    printfn "=== union %s ===" spec.Name

    for l in spec.Layouts do
        printfn "Cases [%A]:" l.Range
        for a in l.Arms do
            printfn "  [%A] -> %s" a.Keys a.Name
            for e in a.Entries do
                printEntry 2 e

    printfn ""

let printPacket (spec: PacketSpec) =
    printfn "=== packet %s | %A %A | %A ===" spec.ClassName spec.State spec.Direction spec.Since

    printfn "API:"
    for f in spec.ApiFields do
        printField 1 f

    for l in spec.Layouts do
        printfn "Wire [%A]:" l.Range
        for e in l.Entries do
            printEntry 1 e

    printfn ""

let printProtocol (spec: ProtocolSpec) =
    printfn "===== NAMED TYPES ====="
    for t in spec.Types do
        printNamedType t

    printfn "===== UNIONS ====="
    for u in spec.Unions do
        printUnion u

    printfn "===== PACKETS ====="
    for p in spec.Packets do
        printPacket p


[<EntryPoint>]
let main _ =
    printProtocol protocol
    0

