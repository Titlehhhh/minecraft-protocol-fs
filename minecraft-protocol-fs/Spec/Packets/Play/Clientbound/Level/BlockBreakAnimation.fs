namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BlockBreakAnimation =

    let blockBreakAnimation =
        packet "BlockBreakAnimationPacket" Play Clientbound All {
            api [
                field "EntityId"     TInt                All
                field "Location"     (TNamed "Position") All
                field "DestroyStage" TInt                All
            ]

            wire All [
                read "entityId"     VarInt             "EntityId"
                read "location"     (Named "Position") "Location"
                read "destroyStage" I8                 "DestroyStage"
            ]
        }
