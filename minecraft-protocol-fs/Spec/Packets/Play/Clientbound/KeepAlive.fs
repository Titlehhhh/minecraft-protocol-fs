namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KeepAlive =

    let keepAlive =
        packet "KeepAlivePacket" Play Clientbound All {
            api [
                field "KeepAliveId" TLong All
            ]

            wire All [
                read "keepAliveId" I64 "KeepAliveId"
            ]
        }
