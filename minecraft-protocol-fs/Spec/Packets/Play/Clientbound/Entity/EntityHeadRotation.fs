namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityHeadRotation =

    let entityHeadRotation =
        packet "EntityHeadRotationPacket" Play Clientbound All {
            api [
                field "EntityId" TInt All
                field "HeadYaw"  TInt All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "headYaw"  I8     "HeadYaw"
            ]
        }
