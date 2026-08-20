namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GameRuleValues =

    let gameRuleValues =
        packet "GameRuleValuesPacket" Play Clientbound (Since 775) {
            api [ field "Values" (TArray(TNamed "GameRule")) All ]
            wire (Since 775) [ read "values" (Array(Named "GameRule", VarIntCount)) "Values" ]
        }
