namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module InitializeWorldBorder =

    let initializeWorldBorder =
        packet "InitializeWorldBorderPacket" Play Clientbound (Since 755) {
            api [
                field "X"                      TDouble All
                field "Z"                      TDouble All
                field "OldDiameter"            TDouble All
                field "NewDiameter"            TDouble All
                field "Speed"                  TLong   All
                field "PortalTeleportBoundary" TInt    All
                field "WarningBlocks"          TInt    All
                field "WarningTime"            TInt    All
            ]

            wire (Between(755, 758)) [
                read "x"                      F64     "X"
                read "z"                      F64     "Z"
                read "oldDiameter"            F64     "OldDiameter"
                read "newDiameter"            F64     "NewDiameter"
                read "speed"                  VarLong "Speed"
                read "portalTeleportBoundary" VarInt  "PortalTeleportBoundary"
                read "warningBlocks"          VarInt  "WarningBlocks"
                read "warningTime"            VarInt  "WarningTime"
            ]

            wire (Since 759) [
                read "x"                      F64    "X"
                read "z"                      F64    "Z"
                read "oldDiameter"            F64    "OldDiameter"
                read "newDiameter"            F64    "NewDiameter"
                read "speed"                  VarInt "Speed"
                read "portalTeleportBoundary" VarInt "PortalTeleportBoundary"
                read "warningBlocks"          VarInt "WarningBlocks"
                read "warningTime"            VarInt "WarningTime"
            ]
        }
