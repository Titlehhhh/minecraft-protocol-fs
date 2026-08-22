namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerChat =

    let playerChat =
        packet "PlayerChatPacket" Play Clientbound (Since 759) {
            api [
                field "SignedChatContent"      TString                            (Between(759, 759))
                field "SenderName"             TString                            (Between(759, 759))
                field "SenderTeam"             (TOption TString)                  (Between(759, 759))
                field "PreviousSignature"      (TOption TBytes)                   (Between(760, 760))
                field "FormattedMessage"       (TOption TString)                  (Between(760, 760))
                field "GlobalIndex"            TInt                               (Since 770)
                field "SenderUuid"             TUuid                              All
                field "Index"                  TInt                               (Since 761)
                field "Signature"              (TOption TBytes)                   All
                field "PlainMessage"           TString                            (Since 760)
                field "Timestamp"              TLong                              All
                field "Salt"                   TLong                              All
                field "PreviousMessages"       (TArray(TNamed "PreviousMessage")) (Since 760)
                field "UnsignedChatContentJson" (TOption TString)                 (Until 764)
                field "UnsignedChatContent"    (TOption TNbt)                     (Since 765)
                field "FilterType"             TInt                               (Since 760)
                field "FilterTypeMask"         (TOption(TArray TLong))            (Since 760)
                field "Type"                   TInt                               (Until 766)
                field "ChatType"               (THolder(TNamed "ChatTypes"))      (Since 767)
                field "NetworkNameJson"        TString                            (Between(760, 764))
                field "NetworkName"            TNbt                               (Since 765)
                field "NetworkTargetNameJson"  (TOption TString)                  (Between(760, 764))
                field "NetworkTargetName"      (TOption TNbt)                     (Since 765)
            ]

            wire (Between(759, 759)) [
                read "signedChatContent"   Str            "SignedChatContent"
                read "unsignedChatContent" (Option Str)   "UnsignedChatContentJson"
                read "type"                VarInt         "Type"
                read "senderUuid"          Uuid           "SenderUuid"
                read "senderName"          Str            "SenderName"
                read "senderTeam"          (Option Str)   "SenderTeam"
                read "timestamp"           I64            "Timestamp"
                read "salt"                I64            "Salt"
                read "signature"           ByteArray      "Signature"
            ]

            wire (Between(760, 760)) [
                read    "previousSignature"  (Option ByteArray)                        "PreviousSignature"
                read    "senderUuid"         Uuid                                      "SenderUuid"
                read    "signature"          ByteArray                                 "Signature"
                read    "plainMessage"       Str                                       "PlainMessage"
                read    "formattedMessage"   (Option Str)                              "FormattedMessage"
                read    "timestamp"          I64                                       "Timestamp"
                read    "salt"               I64                                       "Salt"
                read    "previousMessages"   (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read    "unsignedChatContent" (Option Str)                             "UnsignedChatContentJson"
                read    "filterType"         VarInt                                    "FilterType"
                readOpt "filterTypeMask"     (Array(I64, VarIntCount))                 "FilterTypeMask" "filterType" [ 2 ]
                read    "type"               VarInt                                    "Type"
                read    "networkName"        Str                                       "NetworkNameJson"
                read    "networkTargetName"  (Option Str)                              "NetworkTargetNameJson"
            ]

            wire (Between(761, 764)) [
                read    "senderUuid"          Uuid                                         "SenderUuid"
                read    "index"               VarInt                                       "Index"
                read    "signature"           (Option(FixedBytes 256))                     "Signature"
                read    "plainMessage"        Str                                          "PlainMessage"
                read    "timestamp"           I64                                          "Timestamp"
                read    "salt"                I64                                          "Salt"
                read    "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read    "unsignedChatContent" (Option Str)                                 "UnsignedChatContentJson"
                read    "filterType"          VarInt                                       "FilterType"
                readOpt "filterTypeMask"      (Array(I64, VarIntCount))                    "FilterTypeMask" "filterType" [ 2 ]
                read    "type"                VarInt                                       "Type"
                read    "networkName"         Str                                          "NetworkNameJson"
                read    "networkTargetName"   (Option Str)                                 "NetworkTargetNameJson"
            ]

            wire (Between(765, 766)) [
                read    "senderUuid"          Uuid                                         "SenderUuid"
                read    "index"               VarInt                                       "Index"
                read    "signature"           (Option(FixedBytes 256))                     "Signature"
                read    "plainMessage"        Str                                          "PlainMessage"
                read    "timestamp"           I64                                          "Timestamp"
                read    "salt"                I64                                          "Salt"
                read    "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read    "unsignedChatContent" (Option AnonNbt)                             "UnsignedChatContent"
                read    "filterType"          VarInt                                       "FilterType"
                readOpt "filterTypeMask"      (Array(I64, VarIntCount))                    "FilterTypeMask" "filterType" [ 2 ]
                read    "type"                VarInt                                       "Type"
                read    "networkName"         AnonNbt                                      "NetworkName"
                read    "networkTargetName"   (Option AnonNbt)                             "NetworkTargetName"
            ]

            wire (Between(767, 769)) [
                read    "senderUuid"          Uuid                                         "SenderUuid"
                read    "index"               VarInt                                       "Index"
                read    "signature"           (Option(FixedBytes 256))                     "Signature"
                read    "plainMessage"        Str                                          "PlainMessage"
                read    "timestamp"           I64                                          "Timestamp"
                read    "salt"                I64                                          "Salt"
                read    "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read    "unsignedChatContent" (Option AnonNbt)                             "UnsignedChatContent"
                read    "filterType"          VarInt                                       "FilterType"
                readOpt "filterTypeMask"      (Array(I64, VarIntCount))                    "FilterTypeMask" "filterType" [ 2 ]
                read    "type"                (RegistryHolder(Named "ChatTypes"))          "ChatType"
                read    "networkName"         AnonNbt                                      "NetworkName"
                read    "networkTargetName"   (Option AnonNbt)                             "NetworkTargetName"
            ]

            wire (Since 770) [
                read    "globalIndex"         VarInt                                       "GlobalIndex"
                read    "senderUuid"          Uuid                                         "SenderUuid"
                read    "index"               VarInt                                       "Index"
                read    "signature"           (Option(FixedBytes 256))                     "Signature"
                read    "plainMessage"        Str                                          "PlainMessage"
                read    "timestamp"           I64                                          "Timestamp"
                read    "salt"                I64                                          "Salt"
                read    "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                read    "unsignedChatContent" (Option AnonNbt)                             "UnsignedChatContent"
                read    "filterType"          VarInt                                       "FilterType"
                readOpt "filterTypeMask"      (Array(I64, VarIntCount))                    "FilterTypeMask" "filterType" [ 2 ]
                read    "type"                (RegistryHolder(Named "ChatTypes"))          "ChatType"
                read    "networkName"         AnonNbt                                      "NetworkName"
                read    "networkTargetName"   (Option AnonNbt)                             "NetworkTargetName"
            ]
        }
