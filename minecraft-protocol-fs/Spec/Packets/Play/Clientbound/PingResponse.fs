namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingResponse =

    let pingResponse =
        packet "PingResponsePacket" Play Clientbound (Since 764) {
            api [
                field "Id" TLong All
            ]

            wire (Since 764) [
                read "id" I64 "Id"
            ]
        }
