namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerlistHeader =

    let playerlistHeader =
        packet "PlayerlistHeaderPacket" Play Clientbound All {
            api [
                field "HeaderJson" TString (Until 764)
                field "Header"     TNbt    (Since 765)
                field "FooterJson" TString (Until 764)
                field "Footer"     TNbt    (Since 765)
            ]

            wire (Until 764) [
                read "header" Str "HeaderJson"
                read "footer" Str "FooterJson"
            ]

            wire (Since 765) [
                read "header" AnonNbt "Header"
                read "footer" AnonNbt "Footer"
            ]
        }
