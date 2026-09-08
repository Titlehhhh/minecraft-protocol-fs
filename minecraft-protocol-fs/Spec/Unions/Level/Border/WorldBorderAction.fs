namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderAction =

    let worldBorderAction =
        unionType "WorldBorderAction" {
            cases
                (Until 754)
                [
                    case1 0 "SetSize" [ read "radius" F64 "Diameter" ]

                    case1
                        1
                        "LerpSize"
                        [
                            read "old_radius" F64 "OldDiameter"
                            read "new_radius" F64 "NewDiameter"
                            read "speed" VarLong "Speed"
                        ]

                    case1 2 "SetCenter" [ read "x" F64 "X"; read "z" F64 "Z" ]

                    case1
                        3
                        "Initialize"
                        [
                            read "x" F64 "X"
                            read "z" F64 "Z"
                            read "old_radius" F64 "OldDiameter"
                            read "new_radius" F64 "NewDiameter"
                            read "speed" VarLong "Speed"
                            read "portalBoundary" VarInt "PortalTeleportBoundary"
                            read "warning_time" VarInt "WarningTime"
                            read "warning_blocks" VarInt "WarningBlocks"
                        ]

                    case1 4 "SetWarningTime" [ read "warning_time" VarInt "WarningTime" ]

                    case1 5 "SetWarningBlocks" [ read "warning_blocks" VarInt "WarningBlocks" ]
                ]
        }
