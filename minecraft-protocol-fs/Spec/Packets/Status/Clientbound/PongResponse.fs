namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PongResponse =

    let pongResponse =
        packet "PongResponsePacket" Status Clientbound All {
            api [
                field "Time" TLong All
            ]

            wire All [
                read "time" I64 "Time"
            ]
        }
