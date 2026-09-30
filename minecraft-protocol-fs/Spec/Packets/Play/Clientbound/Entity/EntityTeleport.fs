namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityTeleport =

    let entityTeleport =
        packet "EntityTeleportPacket" Play Clientbound All {
            api [
                field "EntityId"  TInt                               All
                field "X"         TDouble                            All
                field "Y"         TDouble                            All
                field "Z"         TDouble                            All
                field "YawByte"   TInt                               (Until 767)
                field "PitchByte" TInt                               (Until 767)
                field "Dx"        TDouble                            (Since 768)
                field "Dy"        TDouble                            (Since 768)
                field "Dz"        TDouble                            (Since 768)
                field "Yaw"       TFloat                             (Since 768)
                field "Pitch"     TFloat                             (Since 768)
                field "Flags"     (TNamed "PositionUpdateRelatives") (Since 768)
                field "OnGround"  TBool                              All
            ]

            wire (Until 767) [
                read "entityId" VarInt "EntityId"
                read "x"        F64    "X"
                read "y"        F64    "Y"
                read "z"        F64    "Z"
                read "yaw"      I8     "YawByte"
                read "pitch"    I8     "PitchByte"
                read "onGround" Bool   "OnGround"
            ]

            wire (Since 768) [
                read "entityId" VarInt                            "EntityId"
                read "x"        F64                               "X"
                read "y"        F64                               "Y"
                read "z"        F64                               "Z"
                read "dx"       F64                               "Dx"
                read "dy"       F64                               "Dy"
                read "dz"       F64                               "Dz"
                read "yaw"      F32                               "Yaw"
                read "pitch"    F32                               "Pitch"
                read "flags"    (Named "PositionUpdateRelatives") "Flags"
                read "onGround" Bool                              "OnGround"
            ]
        }
