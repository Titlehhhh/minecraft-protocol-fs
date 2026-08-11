namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CookieResponsePlay =

    let cookieResponsePlay =
        packet "CookieResponsePacket" Play Serverbound (Since 766) {
            api [
                field "Key"   TString          All
                field "Value" (TOption TBytes) All
            ]

            wire (Since 766) [
                read "key"   Str                "Key"
                read "value" (Option ByteArray) "Value"
            ]
        }
