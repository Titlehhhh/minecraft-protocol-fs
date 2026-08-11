namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SteerBoat =

    let steerBoat =
        packet "SteerBoatPacket" Play Serverbound All {
            api [
                field "LeftPaddle"  TBool All
                field "RightPaddle" TBool All
            ]

            wire All [
                read "leftPaddle"  Bool "LeftPaddle"
                read "rightPaddle" Bool "RightPaddle"
            ]
        }
