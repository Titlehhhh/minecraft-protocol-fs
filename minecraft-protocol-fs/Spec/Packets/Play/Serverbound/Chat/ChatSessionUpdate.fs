namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatSessionUpdate =

    let chatSessionUpdate =
        packet "ChatSessionUpdatePacket" Play Serverbound (Since 761) {
            api [
                field "SessionUuid" TUuid  All
                field "ExpireTime"  TLong  All
                field "PublicKey"   TBytes All
                field "Signature"   TBytes All
            ]

            wire (Since 761) [
                read "sessionUUID" Uuid      "SessionUuid"
                read "expireTime"  I64       "ExpireTime"
                read "publicKey"   ByteArray "PublicKey"
                read "signature"   ByteArray "Signature"
            ]
        }
