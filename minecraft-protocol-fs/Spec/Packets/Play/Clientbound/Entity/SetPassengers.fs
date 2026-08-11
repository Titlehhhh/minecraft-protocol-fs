namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetPassengers =

    let setPassengers =
        packet "SetPassengersPacket" Play Clientbound All {
            api [
                field "EntityId"   TInt          All
                field "Passengers" (TArray TInt) All
            ]

            wire All [
                read "entityId"   VarInt                       "EntityId"
                read "passengers" (Array(VarInt, VarIntCount)) "Passengers"
            ]
        }
