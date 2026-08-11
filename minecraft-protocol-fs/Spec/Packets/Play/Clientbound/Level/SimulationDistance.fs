namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SimulationDistance =

    let simulationDistance =
        packet "SimulationDistancePacket" Play Clientbound (Since 757) {
            api [
                field "Distance" TInt All
            ]

            wire (Since 757) [
                read "distance" VarInt "Distance"
            ]
        }
