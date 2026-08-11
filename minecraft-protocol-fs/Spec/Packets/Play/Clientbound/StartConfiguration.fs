namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module StartConfiguration =

    let startConfiguration =
        packet "StartConfigurationPacket" Play Clientbound (Since 764) {
            api []
            wire (Since 764) []
        }
