namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityVelocity =

    let entityVelocity =
        packet "EntityVelocityPacket" Play Clientbound All {
            api [
                field "EntityId"  TInt All
                field "VelocityX" TInt All
                field "VelocityY" TInt All
                field "VelocityZ" TInt All
            ]

            wire All [
                read "entityId"  VarInt "EntityId"
                read "velocityX" I16    "VelocityX"
                read "velocityY" I16    "VelocityY"
                read "velocityZ" I16    "VelocityZ"
            ]
        }
