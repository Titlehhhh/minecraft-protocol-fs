namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerData =

    let serverData =
        packet "ServerDataPacket" Play Clientbound (Since 759) {
            api [
                field "MotdJson"           (TOption TString) (Between(759, 764))
                field "Motd"               TNbt              (Since 765)
                field "Icon"               (TOption TString) (Between(759, 761))
                field "IconBytes"          (TOption TBytes)  (Since 762)
                field "PreviewsChat"       TBool             (Between(759, 760))
                field "EnforcesSecureChat" TBool             (Between(760, 765))
            ]

            wire (Between(759, 759)) [
                read "motd"         (Option Str) "MotdJson"
                read "icon"         (Option Str) "Icon"
                read "previewsChat" Bool         "PreviewsChat"
            ]

            wire (Between(760, 760)) [
                read "motd"               (Option Str) "MotdJson"
                read "icon"               (Option Str) "Icon"
                read "previewsChat"       Bool         "PreviewsChat"
                read "enforcesSecureChat" Bool         "EnforcesSecureChat"
            ]

            wire (Between(761, 761)) [
                read "motd"               (Option Str) "MotdJson"
                read "icon"               (Option Str) "Icon"
                read "enforcesSecureChat" Bool         "EnforcesSecureChat"
            ]

            wire (Between(762, 764)) [
                read "motd"               Str                "MotdJson"
                read "iconBytes"          (Option ByteArray) "IconBytes"
                read "enforcesSecureChat" Bool               "EnforcesSecureChat"
            ]

            wire (Between(765, 765)) [
                read "motd"               AnonNbt            "Motd"
                read "iconBytes"          (Option ByteArray) "IconBytes"
                read "enforcesSecureChat" Bool               "EnforcesSecureChat"
            ]

            wire (Since 766) [
                read "motd"      AnonNbt            "Motd"
                read "iconBytes" (Option ByteArray) "IconBytes"
            ]
        }
