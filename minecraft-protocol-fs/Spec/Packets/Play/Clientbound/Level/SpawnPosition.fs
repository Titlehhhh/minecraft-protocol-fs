namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnPosition =

    let spawnPosition =
        packet "SpawnPositionPacket" Play Clientbound All {
            api [
                field "Location" (TNamed "Position") All
                field "Angle"    TFloat              (Since 755)
            ]

            wire (Until 754) [
                read "location" (Named "Position") "Location"
            ]

            wire (Since 755) [
                read "location" (Named "Position") "Location"
                read "angle"    F32                "Angle"
            ]
        }
