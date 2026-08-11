namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityStatus =

    let entityStatus =
        packet "EntityStatusPacket" Play Clientbound All {
            api [
                field "EntityId"     TInt All
                field "EntityStatus" TInt All
            ]

            wire All [
                read "entityId"     I32 "EntityId"
                read "entityStatus" I8  "EntityStatus"
            ]
        }
