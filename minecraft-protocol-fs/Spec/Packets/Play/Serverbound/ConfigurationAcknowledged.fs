namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ConfigurationAcknowledged =

    let configurationAcknowledged =
        packet "ConfigurationAcknowledgedPacket" Play Serverbound (Since 764) {
            api []

            wire (Since 764) []
        }
