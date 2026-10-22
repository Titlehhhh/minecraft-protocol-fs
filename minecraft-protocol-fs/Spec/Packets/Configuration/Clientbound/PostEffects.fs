namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PostEffectsConfiguration =

    let postEffectsConfiguration =
        packet "PostEffectsPacket" Configuration Clientbound (Since 777) {
            api [ field "PostEffects" (TArray TString) All ]

            wire (Since 777) [ read "postEffects" (Array(Str, VarIntCount)) "PostEffects" ]
        }
