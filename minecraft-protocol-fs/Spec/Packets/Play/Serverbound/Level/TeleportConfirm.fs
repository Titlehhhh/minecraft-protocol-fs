namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TeleportConfirm =

    let teleportConfirm =
        packet "TeleportConfirmPacket" Play Serverbound All {
            api [
                field "TeleportId" TInt    All
                field "X"        TDouble (Since 777)
                field "Y"        TDouble (Since 777)
                field "Z"        TDouble (Since 777)
                field "YRot"     TFloat  (Since 777)
                field "XRot"     TFloat  (Since 777)
            ]

            wire (Until 776) [
                read "teleportId" VarInt "TeleportId"
            ]

            wire (Since 777) [
                read "teleportId" VarInt "TeleportId"
                read "x"           F64    "X"
                read "y"           F64    "Y"
                read "z"           F64    "Z"
                read "yRot"        F32    "YRot"
                read "xRot"        F32    "XRot"
            ]
        }
