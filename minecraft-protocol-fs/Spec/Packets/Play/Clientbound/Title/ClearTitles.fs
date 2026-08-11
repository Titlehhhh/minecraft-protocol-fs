namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClearTitles =

    let clearTitles =
        packet "ClearTitlesPacket" Play Clientbound (Since 755) {
            api [
                field "Reset" TBool All
            ]

            wire (Since 755) [
                read "reset" Bool "Reset"
            ]
        }
