namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MessageHeader =

    let messageHeader =
        packet "MessageHeaderPacket" Play Clientbound (Between(760, 760)) {
            api [
                field "PreviousSignature" (TOption TBytes) All
                field "SenderUuid"        TUuid            All
                field "Signature"         TBytes           All
                field "MessageHash"       TBytes           All
            ]

            wire (Between(760, 760)) [
                read "previousSignature" (Option ByteArray) "PreviousSignature"
                read "senderUuid"        Uuid               "SenderUuid"
                read "signature"         ByteArray          "Signature"
                read "messageHash"       ByteArray          "MessageHash"
            ]
        }
