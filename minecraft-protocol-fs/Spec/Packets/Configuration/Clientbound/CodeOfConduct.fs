namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CodeOfConductConfiguration =

    let codeOfConductConfiguration =
        packet "CodeOfConductPacket" Configuration Clientbound (Since 773) {
            api [ field "Contents" TString All ]
            wire (Since 773) [ read "contents" Str "Contents" ]
        }
