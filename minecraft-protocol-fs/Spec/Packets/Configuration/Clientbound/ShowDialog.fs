namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ShowDialogConfiguration =

    let showDialogConfiguration =
        packet "ShowDialogPacket" Configuration Clientbound (Since 771) {
            api [
                field "Dialog" TNbt All
            ]

            wire (Since 771) [
                read "dialog" AnonNbt "Dialog"
            ]
        }
