namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FinishConfigurationServerbound =

    let finishConfigurationServerbound =
        packet "FinishConfigurationPacket" Configuration Serverbound (Since 764) {
            api []
            wire (Since 764) []
        }
