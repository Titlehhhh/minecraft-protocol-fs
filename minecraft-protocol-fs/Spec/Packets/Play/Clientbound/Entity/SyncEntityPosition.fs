namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SyncEntityPosition =

    let syncEntityPosition =
        packet "SyncEntityPositionPacket" Play Clientbound (Since 768) {
            api [
                field "EntityId" TInt    All
                field "X"        TDouble All
                field "Y"        TDouble All
                field "Z"        TDouble All
                field "Dx"       TDouble All
                field "Dy"       TDouble All
                field "Dz"       TDouble All
                field "Yaw"      TFloat  All
                field "Pitch"    TFloat  All
                field "OnGround" TBool   All
            ]

            wire (Since 768) [
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
        }
