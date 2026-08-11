namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module OpenSignEntity =

    let openSignEntity =
        packet "OpenSignEntityPacket" Play Clientbound All {
            api [
                field "Location"    (TNamed "Position") All
                field "IsFrontText" TBool               (Since 763)
            ]

            wire (Until 762) [
                read "location" (Named "Position") "Location"
            ]

            wire (Since 763) [
                read "location"    (Named "Position") "Location"
                read "isFrontText" Bool               "IsFrontText"
            ]
        }
