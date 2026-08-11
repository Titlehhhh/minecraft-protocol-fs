namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerRemove =

    let playerRemove =
        packet "PlayerRemovePacket" Play Clientbound (Since 761) {
            api [
                field "Players" (TArray TUuid) All
            ]

            wire (Since 761) [
                read "players" (Array(Uuid, VarIntCount)) "Players"
            ]
        }
