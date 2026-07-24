namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerPosition =

    let playerPosition =
        packet "PlayerPositionPacket" Play Clientbound All {
            protoId "position"

            api [
                field "X"               TDouble                          All
                field "Y"               TDouble                          All
                field "Z"               TDouble                          All
                field "Yaw"             TFloat                           All
                field "Pitch"           TFloat                           All
                field "Flags"           (TNamed "PositionUpdateRelatives") All
                field "TeleportId"      TInt                             All
                field "DismountVehicle" TBool                            (Between(755, 761))
                field "Dx"              TDouble                          (Since 768)
                field "Dy"              TDouble                          (Since 768)
                field "Dz"              TDouble                          (Since 768)
            ]

            wire (Until 754) [
                read "x"          F64                                  "X"
                read "y"          F64                                  "Y"
                read "z"          F64                                  "Z"
                read "yaw"        F32                                  "Yaw"
                read "pitch"      F32                                  "Pitch"
                read "flags"      (Named "PositionUpdateRelatives")    "Flags"
                read "teleportId" VarInt                               "TeleportId"
            ]

            wire (Between(755, 761)) [
                read "x"               F64                               "X"
                read "y"               F64                               "Y"
                read "z"               F64                               "Z"
                read "yaw"             F32                               "Yaw"
                read "pitch"           F32                               "Pitch"
                read "flags"           (Named "PositionUpdateRelatives") "Flags"
                read "teleportId"      VarInt                            "TeleportId"
                read "dismountVehicle" Bool                              "DismountVehicle"
            ]

            wire (Between(762, 765)) [
                read "x"          F64                                  "X"
                read "y"          F64                                  "Y"
                read "z"          F64                                  "Z"
                read "yaw"        F32                                  "Yaw"
                read "pitch"      F32                                  "Pitch"
                read "flags"      (Named "PositionUpdateRelatives")    "Flags"
                read "teleportId" VarInt                               "TeleportId"
            ]

            wire (Between(766, 767)) [
                read "x"          F64                                  "X"
                read "y"          F64                                  "Y"
                read "z"          F64                                  "Z"
                read "yaw"        F32                                  "Yaw"
                read "pitch"      F32                                  "Pitch"
                read "flags"      (Named "PositionUpdateRelatives")    "Flags"
                read "teleportId" VarInt                               "TeleportId"
            ]

            wire (Since 768) [
                read "teleportId" VarInt                               "TeleportId"
                read "x"          F64                                  "X"
                read "y"          F64                                  "Y"
                read "z"          F64                                  "Z"
                read "dx"         F64                                  "Dx"
                read "dy"         F64                                  "Dy"
                read "dz"         F64                                  "Dz"
                read "yaw"        F32                                  "Yaw"
                read "pitch"      F32                                  "Pitch"
                read "flags"      (Named "PositionUpdateRelatives")    "Flags"
            ]
        }
