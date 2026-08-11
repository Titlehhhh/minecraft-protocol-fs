namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChangeGamemode =

    let changeGamemode =
        packet "ChangeGamemodePacket" Play Serverbound (Since 771) {
            api [
                field "Mode" TInt All
            ]

            wire (Since 771) [
                read "mode" VarInt "Mode"
            ]
        }
