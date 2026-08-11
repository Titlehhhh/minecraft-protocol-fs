namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerLoaded =

    let playerLoaded =
        packet "PlayerLoadedPacket" Play Serverbound (Since 769) {
            api []

            wire (Since 769) []
        }
