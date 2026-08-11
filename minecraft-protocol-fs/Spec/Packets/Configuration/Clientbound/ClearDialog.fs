namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClearDialogConfiguration =

    let clearDialogConfiguration =
        packet "ClearDialogPacket" Configuration Clientbound (Since 771) {
            api []
            wire (Since 771) []
        }
