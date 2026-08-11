namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Chat =

    let chat =
        packet "ChatPacket" Play Clientbound (Until 758) {
            api [
                field "Message"  TString All
                field "Position" TInt    All
                field "Sender"   TUuid   All
            ]

            wire (Until 758) [
                read "message"  Str  "Message"
                read "position" I8   "Position"
                read "sender"   Uuid "Sender"
            ]
        }
