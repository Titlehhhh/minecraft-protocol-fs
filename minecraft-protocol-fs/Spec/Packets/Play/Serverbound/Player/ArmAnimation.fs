namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ArmAnimation =

    let armAnimation =
        packet "ArmAnimationPacket" Play Serverbound All {
            api [
                field "Hand" TInt All
            ]

            wire All [
                read "hand" VarInt "Hand"
            ]
        }
