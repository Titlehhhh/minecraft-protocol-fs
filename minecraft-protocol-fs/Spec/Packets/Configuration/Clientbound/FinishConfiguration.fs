namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FinishConfigurationClientbound =

    let finishConfigurationClientbound =
        packet "FinishConfigurationPacket" Configuration Clientbound (Since 764) {
            api []
            wire (Since 764) []
        }
