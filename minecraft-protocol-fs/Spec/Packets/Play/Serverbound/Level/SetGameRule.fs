namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetGameRule =

    let setGameRule =
        packet "SetGameRulePacket" Play Serverbound (Since 775) {
            api [ field "Entries" (TArray(TNamed "GameRule")) All ]
            wire (Since 775) [ read "entries" (Array(Named "GameRule", VarIntCount)) "Entries" ]
        }
