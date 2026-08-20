namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GameTestHighlightPos =

    let gameTestHighlightPos =
        packet "GameTestHighlightPosPacket" Play Clientbound (Since 773) {
            api
                [
                    field "AbsolutePos" (TNamed "Position") All
                    field "RelativePos" (TNamed "Position") All
                ]

            wire
                (Since 773)
                [
                    read "absolutePos" (Named "Position") "AbsolutePos"
                    read "relativePos" (Named "Position") "RelativePos"
                ]
        }
