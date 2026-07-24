namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EncryptionResponse =

    let encryptionResponse =
        packet "EncryptionResponsePacket" Login Serverbound All {
            protoId "encryption_begin"

            api [
                field "SharedSecret"     TBytes           All
                field "VerifyToken"      (TOption TBytes) All
                field "Salt"             (TOption TLong)  (Between(759, 760))
                field "MessageSignature" (TOption TBytes) (Between(759, 760))
            ]

            wire (Until 758) [
                read "sharedSecret" ByteArray "SharedSecret"
                read "verifyToken"  ByteArray "VerifyToken"
            ]

            wire (Between(759, 760)) [
                read "sharedSecret"      ByteArray "SharedSecret"
                read "hasVerifyToken"    Bool      "_hasVerifyToken"

                inlineUnion "_hasVerifyToken" [
                    case1 1 "VerifyToken" [
                        read "verifyToken" ByteArray "VerifyToken"
                    ]
                    case1 0 "SaltSignature" [
                        read "salt"              I64       "Salt"
                        read "messageSignature"  ByteArray "MessageSignature"
                    ]
                ]
            ]

            wire (Since 761) [
                read "sharedSecret" ByteArray "SharedSecret"
                read "verifyToken"  ByteArray "VerifyToken"
            ]
        }
