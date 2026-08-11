namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AttachEntity =

    let attachEntity =
        packet "AttachEntityPacket" Play Clientbound All {
            api [
                field "EntityId"  TInt All
                field "VehicleId" TInt All
            ]

            wire All [
                read "entityId"  I32 "EntityId"
                read "vehicleId" I32 "VehicleId"
            ]
        }
