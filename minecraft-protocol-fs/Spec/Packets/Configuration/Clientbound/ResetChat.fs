namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResetChat =

    let resetChat =
        packet "ResetChatPacket" Configuration Clientbound (Since 766) {
            api []
            wire (Since 766) []
        }
