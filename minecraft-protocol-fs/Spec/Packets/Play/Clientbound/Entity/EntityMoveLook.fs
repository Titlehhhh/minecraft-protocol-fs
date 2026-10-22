namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityMoveLook =

    let entityMoveLook =
        packet "EntityMoveLookPacket" Play Clientbound All {
            api [
                field "EntityId" TInt                  All
                field "Dx"       TInt                  (Until 776)
                field "Dy"       TInt                  (Until 776)
                field "Dz"       TInt                  (Until 776)
                field "Yaw"      TInt                  All
                field "Pitch"    TInt                  All
                field "OnGround" TBool                 (Until 776)
                field "Move"     (TNamed "EntityDelta") (Since 777)
            ]

            wire (Until 776) [
                read "entityId" VarInt "EntityId"
                read "dX"       I16    "Dx"
                read "dY"       I16    "Dy"
                read "dZ"       I16    "Dz"
                read "yaw"      I8     "Yaw"
                read "pitch"    I8     "Pitch"
                read "onGround" Bool   "OnGround"
            ]

            wire (Since 777) [
                read "entityId" VarInt "EntityId"
                read "move"     (Named "EntityDelta") "Move"
                read "yaw"      I8     "Yaw"
                read "pitch"    I8     "Pitch"
            ]
        }
