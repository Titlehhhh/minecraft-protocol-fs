namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetTitleText =

    let setTitleText =
        packet "SetTitleTextPacket" Play Clientbound (Since 755) {
            api [
                field "TextJson" TString (Between(755, 764))
                field "Text"     TNbt    (Since 765)
            ]

            wire (Between(755, 764)) [ read "text" Str     "TextJson" ]
            wire (Since 765)         [ read "text" AnonNbt "Text" ]
        }
