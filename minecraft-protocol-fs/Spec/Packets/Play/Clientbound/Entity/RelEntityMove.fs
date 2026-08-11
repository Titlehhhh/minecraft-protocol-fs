namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RelEntityMove =

    let relEntityMove =
        packet "RelEntityMovePacket" Play Clientbound All {
            api [
                field "EntityId" TInt  All
                field "Dx"       TInt  All
                field "Dy"       TInt  All
                field "Dz"       TInt  All
                field "OnGround" TBool All
            ]

            wire All [
                read "entityId" VarInt "EntityId"
                read "dX"       I16    "Dx"
                read "dY"       I16    "Dy"
                read "dZ"       I16    "Dz"
                read "onGround" Bool   "OnGround"
            ]
        }
