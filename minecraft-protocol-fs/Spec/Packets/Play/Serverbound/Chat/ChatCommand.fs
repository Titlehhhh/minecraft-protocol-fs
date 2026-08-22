namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatCommand =

    let chatCommand =
        packet "ChatCommandPacket" Play Serverbound (Since 766) {
            api [ field "Command" TString All ]

            wire (Since 766) [ read "command" Str "Command" ]
        }
