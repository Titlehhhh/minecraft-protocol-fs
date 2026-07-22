namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LegacyServerListPing =

    let legacyServerListPing =
        packet "LegacyServerListPingPacket" Handshaking Serverbound All {
            api [
                field "Payload" TInt All
            ]

            wire All [
                read "payload" U8 "Payload"
            ]
        }
