namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingPlay =

    let pingPlay =
        packet "PingPacket" Play Clientbound (Since 755) {
            api [
                field "Id" TInt All
            ]

            wire (Since 755) [
                read "id" I32 "Id"
            ]
        }
