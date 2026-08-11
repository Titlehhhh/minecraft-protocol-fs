namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CookieRequestConfiguration =

    let cookieRequestConfiguration =
        packet "CookieRequestPacket" Configuration Clientbound (Since 766) {
            api [
                field "Cookie" TString All
            ]

            wire (Since 766) [
                read "cookie" Str "Cookie"
            ]
        }
