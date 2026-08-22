namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ProfilelessChat =

    let profilelessChat =
        packet "ProfilelessChatPacket" Play Clientbound (Since 761) {
            api [
                field "MessageJson" TString                       (Until 764)
                field "Message"     TNbt                          (Since 765)
                field "Type"        TInt                          (Until 766)
                field "ChatType"    (THolder(TNamed "ChatTypes"))  (Since 767)
                field "NameJson"    TString                       (Until 764)
                field "Name"        TNbt                          (Since 765)
                field "TargetJson"  (TOption TString)             (Until 764)
                field "Target"      (TOption TNbt)                (Since 765)
            ]

            wire (Between(761, 764)) [
                read "message" Str          "MessageJson"
                read "type"    VarInt       "Type"
                read "name"    Str          "NameJson"
                read "target"  (Option Str) "TargetJson"
            ]

            wire (Between(765, 766)) [
                read "message" AnonNbt          "Message"
                read "type"    VarInt           "Type"
                read "name"    AnonNbt          "Name"
                read "target"  (Option AnonNbt) "Target"
            ]

            wire (Since 767) [
                read "message" AnonNbt                             "Message"
                read "type"    (RegistryHolder(Named "ChatTypes")) "ChatType"
                read "name"    AnonNbt                             "Name"
                read "target"  (Option AnonNbt)                    "Target"
            ]
        }
