namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BlockAction =

    let blockAction =
        packet "BlockActionPacket" Play Clientbound All {
            api [
                field "Location" (TNamed "Position") All
                field "Byte1"    TInt                All
                field "Byte2"    TInt                All
                field "BlockId"  TInt                All
            ]

            wire All [
                read "location" (Named "Position") "Location"
                read "byte1"    U8                 "Byte1"
                read "byte2"    U8                 "Byte2"
                read "blockId"  VarInt             "BlockId"
            ]
        }
