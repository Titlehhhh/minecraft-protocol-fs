namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnEntityPainting =

    let spawnEntityPainting =
        packet "SpawnEntityPaintingPacket" Play Clientbound (Until 758) {
            api [
                field "EntityId"   TInt                All
                field "EntityUuid" TUuid               All
                field "Title"      TInt                All
                field "Location"   (TNamed "Position") All
                field "Direction"  TInt                All
            ]

            wire (Until 758) [
                read "entityId"   VarInt             "EntityId"
                read "entityUUID" Uuid               "EntityUuid"
                read "title"      VarInt             "Title"
                read "location"   (Named "Position") "Location"
                read "direction"  U8                 "Direction"
            ]
        }
