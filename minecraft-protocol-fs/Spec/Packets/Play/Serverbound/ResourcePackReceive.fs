namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResourcePackReceivePlay =

    let resourcePackReceivePlay =
        packet "ResourcePackReceivePacket" Play Serverbound All {
            api [
                field "Uuid"   TUuid (Since 765)
                field "Result" TInt  All
            ]

            wire (Until 764) [
                read "result" VarInt "Result"
            ]

            wire (Since 765) [
                read "uuid"   Uuid   "Uuid"
                read "result" VarInt "Result"
            ]
        }
