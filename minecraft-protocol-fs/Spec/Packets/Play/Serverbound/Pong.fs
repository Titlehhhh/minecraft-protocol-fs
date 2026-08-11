namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PongPlay =

    let pongPlay =
        packet "PongPacket" Play Serverbound (Since 755) {
            api [
                field "Id" TInt All
            ]

            wire (Since 755) [
                read "id" I32 "Id"
            ]
        }
