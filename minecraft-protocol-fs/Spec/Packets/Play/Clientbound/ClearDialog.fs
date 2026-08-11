namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClearDialog =

    let clearDialog =
        packet "ClearDialogPacket" Play Clientbound (Since 771) {
            api []
            wire (Since 771) []
        }
