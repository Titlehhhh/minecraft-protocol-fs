namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FeatureFlags =

    let featureFlags =
        packet "FeatureFlagsPacket" Play Clientbound (Between(761, 763)) {
            api [ field "Features" (TArray TString) All ]
            wire (Between(761, 763)) [ read "features" (Array(Str, VarIntCount)) "Features" ]
        }
