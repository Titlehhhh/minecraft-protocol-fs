namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityMoveLook =

    let entityMoveLook =
        packet "EntityMoveLookPacket" Play Clientbound All {
            api [
                field "EntityId" TInt  All
                field "Dx"       TInt  All
                field "Dy"       TInt  All
                field "Dz"       TInt  All
                field "Yaw"      TInt  All
                field "Pitch"    TInt  All
                field "OnGround" TBool All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "dX"       I16    "Dx"
                read "dY"       I16    "Dy"
                read "dZ"       I16    "Dz"
                read "yaw"      I8     "Yaw"
                read "pitch"    I8     "Pitch"
                read "onGround" Bool   "OnGround"
            ]
        }
