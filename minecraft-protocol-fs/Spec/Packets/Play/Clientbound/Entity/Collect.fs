namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Collect =

    let collect =
        packet "CollectPacket" Play Clientbound All {
            api [
                field "CollectedEntityId" TInt All
                field "CollectorEntityId" TInt All
                field "PickupItemCount"   TInt All
            ]

            wire All [
                read "collectedEntityId" VarInt "CollectedEntityId"
                read "collectorEntityId" VarInt "CollectorEntityId"
                read "pickupItemCount"   VarInt "PickupItemCount"
            ]
        }
