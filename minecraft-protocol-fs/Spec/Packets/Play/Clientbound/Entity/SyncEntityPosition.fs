namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SyncEntityPosition =

    let syncEntityPosition =
        packet "SyncEntityPositionPacket" Play Clientbound (Since 768) {
            api [
                field "EntityId" TInt                   All
                field "X"        TDouble                (Until 776)
                field "Y"        TDouble                (Until 776)
                field "Z"        TDouble                (Until 776)
                field "Dx"       TDouble                (Until 776)
                field "Dy"       TDouble                (Until 776)
                field "Dz"       TDouble                (Until 776)
                field "Position" (TUnion "SyncEntityPositionKind") (Since 777)
                field "Yaw"      TFloat                 All
                field "Pitch"    TFloat                 All
                field "OnGround" TBool                  All
            ]

            wire (Between(768, 776)) [
                read "entityId" VarInt "EntityId"
                read "x"        F64    "X"
                read "y"        F64    "Y"
                read "z"        F64    "Z"
                read "dx"       F64    "Dx"
                read "dy"       F64    "Dy"
                read "dz"       F64    "Dz"
                read "yaw"      F32    "Yaw"
                read "pitch"    F32    "Pitch"
                read "onGround" Bool   "OnGround"
            ]

            wire (Since 777) [
                read "entityId"    VarInt "EntityId"
                read "positionType" VarInt "_positionType"

                readUnion "_positionType" "SyncEntityPositionKind" "Position"

                read "yaw"      F32    "Yaw"
                read "pitch"    F32    "Pitch"
                read "onGround" Bool   "OnGround"
            ]
        }
