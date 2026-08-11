namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module NamedEntitySpawn =

    let namedEntitySpawn =
        packet "NamedEntitySpawnPacket" Play Clientbound (Until 763) {
            api [
                field "EntityId"   TInt    All
                field "PlayerUuid" TUuid   All
                field "X"          TDouble All
                field "Y"          TDouble All
                field "Z"          TDouble All
                field "Yaw"        TInt    All
                field "Pitch"      TInt    All
            ]

            wire (Until 763) [
                read "entityId"   VarInt "EntityId"
                read "playerUUID" Uuid   "PlayerUuid"
                read "x"          F64    "X"
                read "y"          F64    "Y"
                read "z"          F64    "Z"
                read "yaw"        I8     "Yaw"
                read "pitch"      I8     "Pitch"
            ]
        }
