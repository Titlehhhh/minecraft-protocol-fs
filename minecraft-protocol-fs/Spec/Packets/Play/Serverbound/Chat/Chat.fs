namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatServerbound =

    let chatServerbound =
        packet "ChatPacket" Play Serverbound (Until 758) {
            api [
                field "Message" TString All
            ]

            wire (Until 758) [
                read "message" Str "Message"
            ]
        }
