namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CookieResponseConfiguration =

    let cookieResponseConfiguration =
        packet "CookieResponsePacket" Configuration Serverbound (Since 766) {
            api [ field "Key" TString All; field "Value" (TOption TBytes) All ]

            wire (Between(766, 771)) [ read "key" Str "Key"; read "value" (Option ByteArray) "Value" ]

            wire (Between(772, 772)) [ read "key" Str "Key"; read "value" ByteArray "Value" ]

            wire (Since 773) [ read "key" Str "Key"; read "value" (Option ByteArray) "Value" ]
        }
