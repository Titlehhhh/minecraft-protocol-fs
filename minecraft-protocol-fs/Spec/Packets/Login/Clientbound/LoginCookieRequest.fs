namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginCookieRequest =

    let loginCookieRequest =
        packet "LoginCookieRequestPacket" Login Clientbound (Since 766) {
            protoId "cookie_request"

            api [
                field "Cookie" TString All
            ]

            wire (Since 766) [
                read "cookie" Str "Cookie"
            ]
        }
