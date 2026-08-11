namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldEvent =

    let worldEvent =
        packet "WorldEventPacket" Play Clientbound All {
            api [
                field "EffectId" TInt                All
                field "Location" (TNamed "Position") All
                field "Data"     TInt                All
                field "Global"   TBool               All
            ]

            wire All [
                read "effectId" I32                "EffectId"
                read "location" (Named "Position") "Location"
                read "data"     I32                "Data"
                read "global"   Bool               "Global"
            ]
        }
