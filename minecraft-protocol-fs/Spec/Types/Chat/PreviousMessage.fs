namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PreviousMessage =

    let previousMessage =
        namedType "PreviousMessage" {
            api [
                field "MessageSender"    TUuid            (Between(760, 760))
                field "MessageSignature" TBytes           (Between(760, 760))
                field "Id"               TInt             (Since 761)
                field "Signature"        (TOption TBytes) (Since 761)
            ]

            wire (Between(760, 760)) [
                read "messageSender"    Uuid      "MessageSender"
                read "messageSignature" ByteArray "MessageSignature"
            ]

            wire (Since 761) [
                read    "id"        VarInt           "Id"
                readOpt "signature" (FixedBytes 256) "Signature" "id" [ 0 ]
            ]
        }
