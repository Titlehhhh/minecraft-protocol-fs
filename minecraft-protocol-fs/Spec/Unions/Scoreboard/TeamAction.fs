namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TeamAction =

    let teamAction =
        unionType "TeamAction" {
            cases
                (Until 764)
                [
                    case1
                        0
                        "Created"
                        [
                            read "name" Str "Name"
                            read "friendlyFire" I8 "FriendlyFire"
                            read "nameTagVis" Str "NameTagVisibility"
                            read "collisionRule" Str "CollisionRule"
                            read "formatting" VarInt "Formatting"
                            read "prefix" Str "Prefix"
                            read "suffix" Str "Suffix"
                            read "players" (Array(Str, VarIntCount)) "Players"
                        ]

                    case1 1 "Removed" []

                    case1
                        2
                        "Updated"
                        [
                            read "name" Str "Name"
                            read "friendlyFire" I8 "FriendlyFire"
                            read "nameTagVis" Str "NameTagVisibility"
                            read "collisionRule" Str "CollisionRule"
                            read "formatting" VarInt "Formatting"
                            read "prefix" Str "Prefix"
                            read "suffix" Str "Suffix"
                        ]

                    case1 3 "PlayersAdded" [ read "players" (Array(Str, VarIntCount)) "Players" ]

                    case1 4 "PlayersRemoved" [ read "players" (Array(Str, VarIntCount)) "Players" ]
                ]

            cases
                (Between(771, 775))
                [
                    case1
                        0
                        "Created"
                        [
                            read "name" AnonNbt "Name"
                            read "flags" (Named "TeamFlags") "Flags"
                            read "nameTagVis" VarInt "NameTagVisibility"
                            read "collisionRule" VarInt "CollisionRule"
                            read "formatting" VarInt "Formatting"
                            read "prefix" AnonNbt "Prefix"
                            read "suffix" AnonNbt "Suffix"
                            read "players" (Array(Str, VarIntCount)) "Players"
                        ]

                    case1 1 "Removed" []

                    case1
                        2
                        "Updated"
                        [
                            read "name" AnonNbt "Name"
                            read "flags" (Named "TeamFlags") "Flags"
                            read "nameTagVis" VarInt "NameTagVisibility"
                            read "collisionRule" VarInt "CollisionRule"
                            read "formatting" VarInt "Formatting"
                            read "prefix" AnonNbt "Prefix"
                            read "suffix" AnonNbt "Suffix"
                        ]

                    case [ 3; 4 ] "PlayersChanged" [ read "players" (Array(Str, VarIntCount)) "Players" ]
                ]

            // 26.2 (776): компоненты идут первыми, цвет стал optional, флаги — последними.
            // Источник: провод с Paper 26.2 + ViaVersion Protocol26_1To26_2 (SET_PLAYER_TEAM);
            // факты для 776 ещё несут раскладку 771.
            cases
                (Since 776)
                [
                    case1
                        0
                        "Created"
                        [
                            read "name" AnonNbt "Name"
                            read "prefix" AnonNbt "Prefix"
                            read "suffix" AnonNbt "Suffix"
                            read "nameTagVis" VarInt "NameTagVisibility"
                            read "collisionRule" VarInt "CollisionRule"
                            read "formatting" (Option VarInt) "Formatting"
                            read "flags" (Named "TeamFlags") "Flags"
                            read "players" (Array(Str, VarIntCount)) "Players"
                        ]

                    case1 1 "Removed" []

                    case1
                        2
                        "Updated"
                        [
                            read "name" AnonNbt "Name"
                            read "prefix" AnonNbt "Prefix"
                            read "suffix" AnonNbt "Suffix"
                            read "nameTagVis" VarInt "NameTagVisibility"
                            read "collisionRule" VarInt "CollisionRule"
                            read "formatting" (Option VarInt) "Formatting"
                            read "flags" (Named "TeamFlags") "Flags"
                        ]

                    case [ 3; 4 ] "PlayersChanged" [ read "players" (Array(Str, VarIntCount)) "Players" ]
                ]
        }
