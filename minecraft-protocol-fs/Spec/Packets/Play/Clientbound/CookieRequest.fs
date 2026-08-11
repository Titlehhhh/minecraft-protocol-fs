namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CookieRequest =

    let cookieRequest =
        packet "CookieRequestPacket" Play Clientbound (Since 766) {
            api [
                field "Cookie" TString All
            ]

            wire (Since 766) [
                read "cookie" Str "Cookie"
            ]
        }
