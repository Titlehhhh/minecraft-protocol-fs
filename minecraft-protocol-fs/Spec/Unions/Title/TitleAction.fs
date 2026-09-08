namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TitleAction =

    let titleAction =
        unionType "TitleAction" {
            cases
                (Until 754)
                [
                    case1 0 "SetTitle" [ read "text" Str "TextJson" ]

                    case1 1 "SetSubtitle" [ read "text" Str "TextJson" ]

                    case1 2 "SetActionBar" [ read "text" Str "TextJson" ]

                    case1
                        3
                        "SetTimes"
                        [
                            read "fadeIn" I32 "FadeIn"
                            read "stay" I32 "Stay"
                            read "fadeOut" I32 "FadeOut"
                        ]
                ]
        }
