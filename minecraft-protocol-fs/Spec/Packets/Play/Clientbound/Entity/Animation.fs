namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Animation =

    let animation =
        packet "AnimationPacket" Play Clientbound All {
            api [
                field "EntityId"  TInt All
                field "Animation" TInt All
            ]

            wire All [
                read "entityId"  VarInt "EntityId"
                read "animation" U8     "Animation"
            ]
        }
