namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginCookieResponse =

    let loginCookieResponse =
        packet "LoginCookieResponsePacket" Login Serverbound (Since 766) {
            protoId "cookie_response"

            api [
                field "Key"   TString          All
                field "Value" (TOption TBytes) All
            ]

            wire (Since 766) [
                read "key"   Str                   "Key"
                read "value" (Option ByteArray)    "Value"
            ]
        }
