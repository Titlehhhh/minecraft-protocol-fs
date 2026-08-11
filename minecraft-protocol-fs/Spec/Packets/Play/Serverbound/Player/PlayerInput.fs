namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerInput =

    let playerInput =
        packet "PlayerInputPacket" Play Serverbound (Since 768) {
            api [
                field "Inputs" (TNamed "PlayerInputFlags") All
            ]

            wire (Since 768) [
                read "inputs" (Named "PlayerInputFlags") "Inputs"
            ]
        }
