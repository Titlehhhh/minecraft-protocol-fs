namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TeamAction =

    let teamAction =
        unionType "TeamAction" {
            cases (Until 764) [
                case1 0 "Created" [
                    read "name"          Str    "Name"
                    read "friendlyFire"  I8     "FriendlyFire"
                    read "nameTagVis"    Str    "NameTagVisibility"
                    read "collisionRule" Str    "CollisionRule"
                    read "formatting"    VarInt "Formatting"
                    read "prefix"        Str    "Prefix"
                    read "suffix"        Str    "Suffix"
                    read "players"       (Array(Str, VarIntCount)) "Players"
                ]

                case1 1 "Removed" []

                case1 2 "Updated" [
                    read "name"          Str    "Name"
                    read "friendlyFire"  I8     "FriendlyFire"
                    read "nameTagVis"    Str    "NameTagVisibility"
                    read "collisionRule" Str    "CollisionRule"
                    read "formatting"    VarInt "Formatting"
                    read "prefix"        Str    "Prefix"
                    read "suffix"        Str    "Suffix"
                ]

                case1 3 "PlayersAdded" [
                    read "players" (Array(Str, VarIntCount)) "Players"
                ]

                case1 4 "PlayersRemoved" [
                    read "players" (Array(Str, VarIntCount)) "Players"
                ]
            ]

            cases (Since 771) [
                case1 0 "Created" [
                    read    "name"          AnonNbt "Name"
                    discard "flags"         U8
                    read    "nameTagVis"    VarInt  "NameTagVisibility"
                    read    "collisionRule" VarInt  "CollisionRule"
                    read    "formatting"    VarInt  "Formatting"
                    read    "prefix"        AnonNbt "Prefix"
                    read    "suffix"        AnonNbt "Suffix"
                    read    "players"       (Array(Str, VarIntCount)) "Players"
                ]

                case1 1 "Removed" []

                case1 2 "Updated" [
                    read    "name"          AnonNbt "Name"
                    discard "flags"         U8
                    read    "nameTagVis"    VarInt  "NameTagVisibility"
                    read    "collisionRule" VarInt  "CollisionRule"
                    read    "formatting"    VarInt  "Formatting"
                    read    "prefix"        AnonNbt "Prefix"
                    read    "suffix"        AnonNbt "Suffix"
                ]

                case [3; 4] "PlayersChanged" [
                    read "players" (Array(Str, VarIntCount)) "Players"
                ]
            ]
        }
