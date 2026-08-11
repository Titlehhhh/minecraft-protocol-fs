namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Camera =

    let camera =
        packet "CameraPacket" Play Clientbound All {
            api [
                field "CameraId" TInt All
            ]

            wire All [
                read "cameraId" VarInt "CameraId"
            ]
        }
