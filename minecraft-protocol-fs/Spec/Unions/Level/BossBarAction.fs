namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BossBarAction =

    let bossBarAction =
        unionType "BossBarAction" {
            cases
                (Until 764)
                [
                    case1
                        0
                        "Add"
                        [
                            read "title" Str "Title"
                            read "health" F32 "Health"
                            read "color" VarInt "Color"
                            read "dividers" VarInt "Dividers"
                            read "flags" U8 "Flags"
                        ]

                    case1 1 "Remove" []

                    case1 2 "UpdateHealth" [ read "health" F32 "Health" ]

                    case1 3 "UpdateTitle" [ read "title" Str "Title" ]

                    case1 4 "UpdateStyle" [ read "color" VarInt "Color"; read "dividers" VarInt "Dividers" ]

                    case1 5 "UpdateFlags" [ read "flags" U8 "Flags" ]
                ]

            cases
                (Since 765)
                [
                    case1
                        0
                        "Add"
                        [
                            read "title" AnonNbt "Title"
                            read "health" F32 "Health"
                            read "color" VarInt "Color"
                            read "dividers" VarInt "Dividers"
                            read "flags" U8 "Flags"
                        ]

                    case1 1 "Remove" []

                    case1 2 "UpdateHealth" [ read "health" F32 "Health" ]

                    case1 3 "UpdateTitle" [ read "title" AnonNbt "Title" ]

                    case1 4 "UpdateStyle" [ read "color" VarInt "Color"; read "dividers" VarInt "Dividers" ]

                    case1 5 "UpdateFlags" [ read "flags" U8 "Flags" ]
                ]
        }
