namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnEntityExperienceOrb =

    let spawnEntityExperienceOrb =
        packet "SpawnEntityExperienceOrbPacket" Play Clientbound (Until 769) {
            api [
                field "EntityId" TInt    All
                field "X"        TDouble All
                field "Y"        TDouble All
                field "Z"        TDouble All
                field "Count"    TInt    All
            ]

            wire (Until 769) [
                read "entityId" VarInt "EntityId"
                read "x"        F64    "X"
                read "y"        F64    "Y"
                read "z"        F64    "Z"
                read "count"    I16    "Count"
            ]
        }
