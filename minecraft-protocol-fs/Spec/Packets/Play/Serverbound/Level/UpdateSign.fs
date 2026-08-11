namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateSign =

    let updateSign =
        packet "UpdateSignPacket" Play Serverbound All {
            api [
                field "Location"    (TNamed "Position") All
                field "IsFrontText" TBool               (Since 763)
                field "Text1"       TString             All
                field "Text2"       TString             All
                field "Text3"       TString             All
                field "Text4"       TString             All
            ]

            wire (Until 762) [
                read "location" (Named "Position") "Location"
                read "text1"    Str                "Text1"
                read "text2"    Str                "Text2"
                read "text3"    Str                "Text3"
                read "text4"    Str                "Text4"
            ]

            wire (Since 763) [
                read "location"    (Named "Position") "Location"
                read "isFrontText" Bool               "IsFrontText"
                read "text1"       Str                "Text1"
                read "text2"       Str                "Text2"
                read "text3"       Str                "Text3"
                read "text4"       Str                "Text4"
            ]
        }
