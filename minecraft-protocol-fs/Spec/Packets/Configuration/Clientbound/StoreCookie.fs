namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module StoreCookieConfiguration =

    let storeCookieConfiguration =
        packet "StoreCookiePacket" Configuration Clientbound (Since 766) {
            api [
                field "Key"   TString All
                field "Value" TBytes  All
            ]

            wire (Since 766) [
                read "key"   Str       "Key"
                read "value" ByteArray "Value"
            ]
        }
