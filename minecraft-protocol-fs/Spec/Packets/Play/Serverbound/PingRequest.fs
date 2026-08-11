namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingRequestPlay =

    let pingRequestPlay =
        packet "PingRequestPacket" Play Serverbound (Since 764) {
            api [
                field "Id" TLong All
            ]

            wire (Since 764) [
                read "id" I64 "Id"
            ]
        }
