namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetTestBlock =

    let setTestBlock =
        packet "SetTestBlockPacket" Play Serverbound (Since 770) {
            api [
                field "Position" (TNamed "Position") All
                field "Mode"     TInt                All
                field "Message"  TString             All
            ]

            wire (Since 770) [
                read "position" (Named "Position") "Position"
                read "mode"     VarInt             "Mode"
                read "message"  Str                "Message"
            ]
        }
