namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityTeleport =

    let entityTeleport =
        packet "EntityTeleportPacket" Play Clientbound All {
            api [
                field "EntityId" TInt    All
                field "X"        TDouble All
                field "Y"        TDouble All
                field "Z"        TDouble All
                field "Yaw"      TInt    All
                field "Pitch"    TInt    All
                field "OnGround" TBool   All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "x"        F64    "X"
                read "y"        F64    "Y"
                read "z"        F64    "Z"
                read "yaw"      I8     "Yaw"
                read "pitch"    I8     "Pitch"
                read "onGround" Bool   "OnGround"
            ]
        }
