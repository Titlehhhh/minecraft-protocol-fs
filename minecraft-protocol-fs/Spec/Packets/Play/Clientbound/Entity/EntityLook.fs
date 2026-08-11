namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityLook =

    let entityLook =
        packet "EntityLookPacket" Play Clientbound All {
            api [
                field "EntityId" TInt  All
                field "Yaw"      TInt  All
                field "Pitch"    TInt  All
                field "OnGround" TBool All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "yaw"      I8     "Yaw"
                read "pitch"    I8     "Pitch"
                read "onGround" Bool   "OnGround"
            ]
        }
