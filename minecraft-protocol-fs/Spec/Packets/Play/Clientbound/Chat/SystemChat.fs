namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SystemChat =

    let systemChat =
        packet "SystemChatPacket" Play Clientbound (Since 759) {
            api [
                field "ContentJson" TString (Between(759, 764))
                field "Content"     TNbt    (Since 765)
                field "Type"        TInt    (Between(759, 759))
                field "IsActionBar" TBool   (Since 760)
            ]

            wire (Between(759, 759)) [
                read "content" Str    "ContentJson"
                read "type"    VarInt "Type"
            ]

            wire (Between(760, 764)) [
                read "content"     Str  "ContentJson"
                read "isActionBar" Bool "IsActionBar"
            ]

            wire (Since 765) [
                read "content"     AnonNbt "Content"
                read "isActionBar" Bool    "IsActionBar"
            ]
        }
