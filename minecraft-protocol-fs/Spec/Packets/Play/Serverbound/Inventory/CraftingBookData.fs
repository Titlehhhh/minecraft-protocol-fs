namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CraftingBookData =

    let craftingBookData =
        packet "CraftingBookDataPacket" Play Serverbound (Until 736) {
            api [ field "Data" (TUnion "CraftingBookDataAction") All ]

            wire (Until 736) [
                read      "type" VarInt "_type"
                readUnion "_type" "CraftingBookDataAction" "Data"
            ]
        }
