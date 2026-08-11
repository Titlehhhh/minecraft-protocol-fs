namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BlockChange =

    let blockChange =
        packet "BlockChangePacket" Play Clientbound All {
            api [
                field "Location" (TNamed "Position") All
                field "Type"     TInt                All
            ]

            wire All [
                read "location" (Named "Position") "Location"
                read "type"     VarInt             "Type"
            ]
        }
