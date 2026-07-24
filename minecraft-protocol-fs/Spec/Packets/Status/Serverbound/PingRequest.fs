namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingRequest =

    let pingRequest =
        packet "PingRequestPacket" Status Serverbound All {
            protoId "ping"

            api [
                field "Time" TLong All
            ]

            wire All [
                read "time" I64 "Time"
            ]
        }
