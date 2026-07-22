namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityMetadata =

    let entityMetadataPacket =
        packet "EntityMetadataPacket" Play Clientbound All {
            api [
                field "EntityId" TInt All
                field "Metadata" (TArray(TNamed "EntityMetadataEntry")) All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "metadata" entityMetadataWire "Metadata"
            ]
        }
