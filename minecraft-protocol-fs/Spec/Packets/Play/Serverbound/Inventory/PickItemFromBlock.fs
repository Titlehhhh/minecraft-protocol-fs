namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PickItemFromBlock =

    let pickItemFromBlock =
        packet "PickItemFromBlockPacket" Play Serverbound (Since 769) {
            api [
                field "Position"    (TNamed "Position") All
                field "IncludeData" TBool               All
            ]

            wire (Since 769) [
                read "position"    (Named "Position") "Position"
                read "includeData" Bool               "IncludeData"
            ]
        }
