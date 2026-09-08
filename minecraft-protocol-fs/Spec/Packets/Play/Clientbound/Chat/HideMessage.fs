namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HideMessage =

    let hideMessage =
        packet "HideMessagePacket" Play Clientbound (Since 760) {
            api [
                field "MessageSignature" TBytes (Between(760, 760))
                field "Id"               TInt (Since 761)
                field "Signature"        (TOption TBytes) (Since 761)
            ]

            wire (Between(760, 760)) [ read "signature" ByteArray "MessageSignature" ]

            wire (Since 761) [
                read    "id" VarInt "Id"
                readOpt "signature" (FixedBytes 256) "Signature" "id" [ 0 ]
            ]
        }
