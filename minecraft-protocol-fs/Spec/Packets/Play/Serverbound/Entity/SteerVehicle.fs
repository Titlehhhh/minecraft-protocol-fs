namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SteerVehicle =

    let steerVehicle =
        packet "SteerVehiclePacket" Play Serverbound (Until 767) {
            api [
                field "Sideways" TFloat All
                field "Forward"  TFloat All
                field "Jump"     TInt   All
            ]

            wire (Until 767) [
                read "sideways" F32 "Sideways"
                read "forward"  F32 "Forward"
                read "jump"     U8  "Jump"
            ]
        }
