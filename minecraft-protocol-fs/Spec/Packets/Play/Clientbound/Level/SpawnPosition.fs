namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnPosition =

    let spawnPosition =
        packet "SpawnPositionPacket" Play Clientbound All {
            api
                [
                    field "Location" (TNamed "Position") (Until 772)
                    field "Angle" TFloat (Between(755, 772))
                    field "RespawnData" (TNamed "RespawnData") (Since 773)
                ]

            wire (Until 754) [ read "location" (Named "Position") "Location" ]

            wire (Between(755, 772)) [ read "location" (Named "Position") "Location"; read "angle" F32 "Angle" ]

            wire (Since 773) [ read "respawnData" (Named "RespawnData") "RespawnData" ]
        }
