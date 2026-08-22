namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChangeGamemode =

    let changeGamemode =
        packet "ChangeGamemodePacket" Play Serverbound (Since 771) {
            api [
                field "Mode" (TEnum "Gamemode") All
            ]

            wire (Since 771) [
                read "mode" (enumAs "Gamemode" VarInt) "Mode"
            ]
        }
