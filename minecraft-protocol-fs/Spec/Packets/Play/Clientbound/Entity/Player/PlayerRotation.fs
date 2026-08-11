namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerRotation =

    let playerRotation =
        packet "PlayerRotationPacket" Play Clientbound (Since 768) {
            api [
                field "Yaw"   TFloat All
                field "Pitch" TFloat All
            ]

            wire (Since 768) [
                read "yaw"   F32 "Yaw"
                read "pitch" F32 "Pitch"
            ]
        }
