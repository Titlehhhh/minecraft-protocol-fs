namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HurtAnimation =

    let hurtAnimation =
        packet "HurtAnimationPacket" Play Clientbound (Since 762) {
            api [
                field "EntityId" TInt All
                field "Yaw"      TFloat All
            ]

            wire (Since 762) [
                read "entityId" VarInt "EntityId"
                read "yaw"      F32    "Yaw"
            ]
        }
