namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerRotation =

    let playerRotation =
        packet "PlayerRotationPacket" Play Clientbound (Since 768) {
            api
                [
                    field "Yaw" TFloat All
                    field "Pitch" TFloat All
                    field "RelativeYaw" TBool (Since 773)
                    field "RelativePitch" TBool (Since 773)
                ]

            wire (Between(768, 772)) [ read "yaw" F32 "Yaw"; read "pitch" F32 "Pitch" ]

            wire
                (Since 773)
                [
                    read "yaw" F32 "Yaw"
                    read "relativeYaw" Bool "RelativeYaw"
                    read "pitch" F32 "Pitch"
                    read "relativePitch" Bool "RelativePitch"
                ]
        }
