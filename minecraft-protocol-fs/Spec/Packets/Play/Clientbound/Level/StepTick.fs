namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module StepTick =

    let stepTick =
        packet "StepTickPacket" Play Clientbound (Since 765) {
            api [
                field "TickSteps" TInt All
            ]

            wire (Since 765) [
                read "tickSteps" VarInt "TickSteps"
            ]
        }
