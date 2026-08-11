namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FeatureFlagsConfiguration =

    let featureFlagsConfiguration =
        packet "FeatureFlagsPacket" Configuration Clientbound (Since 764) {
            api [
                field "Features" (TArray TString) All
            ]

            wire (Since 764) [
                read "features" (Array(Str, VarIntCount)) "Features"
            ]
        }
