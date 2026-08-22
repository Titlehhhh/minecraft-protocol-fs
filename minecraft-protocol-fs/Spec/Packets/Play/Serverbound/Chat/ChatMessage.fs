namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatMessage =

    let chatMessage =
        packet "ChatMessagePacket" Play Serverbound (Since 759) {
            api [
                field "Message"             TString                                 All
                field "Timestamp"           TLong                                   All
                field "Salt"                TLong                                   All
                field "Signature"           (TOption TBytes)                        All
                field "SignedPreview"       TBool                                   (Until 760)
                field "PreviousMessages"    (TArray(TNamed "PreviousMessage"))      (Between(760, 760))
                field "LastRejectedMessage" (TOption(TNamed "LastRejectedMessage")) (Between(760, 760))
                field "Offset"              TInt                                    (Since 761)
                field "Acknowledged"        TBytes                                  (Since 761)
                field "Checksum"            TInt                                    (Since 770)
            ]

            wire (Between(759, 759)) [
                read "message"       Str       "Message"
                read "timestamp"     I64       "Timestamp"
                read "salt"          I64       "Salt"
                read "signature"     ByteArray "Signature"
                read "signedPreview" Bool      "SignedPreview"
            ]

            wire (Between(760, 760)) [
                read "message"             Str                                          "Message"
                read "timestamp"           I64                                          "Timestamp"
                read "salt"                I64                                          "Salt"
                read "signature"           ByteArray                                    "Signature"
                read "signedPreview"       Bool                                         "SignedPreview"
                read "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read "lastRejectedMessage" (Option(Named "LastRejectedMessage"))         "LastRejectedMessage"
            ]

            wire (Between(761, 769)) [
                read "message"      Str                      "Message"
                read "timestamp"    I64                      "Timestamp"
                read "salt"         I64                      "Salt"
                read "signature"    (Option(FixedBytes 256)) "Signature"
                read "offset"       VarInt                   "Offset"
                read "acknowledged" (FixedBytes 3)           "Acknowledged"
            ]

            wire (Since 770) [
                read "message"      Str                      "Message"
                read "timestamp"    I64                      "Timestamp"
                read "salt"         I64                      "Salt"
                read "signature"    (Option(FixedBytes 256)) "Signature"
                read "offset"       VarInt                   "Offset"
                read "acknowledged" (FixedBytes 3)           "Acknowledged"
                read "checksum"     U8                       "Checksum"
            ]
        }
