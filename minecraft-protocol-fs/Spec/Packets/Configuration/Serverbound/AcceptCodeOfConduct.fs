namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AcceptCodeOfConductConfiguration =

    let acceptCodeOfConductConfiguration =
        packet "AcceptCodeOfConductPacket" Configuration Serverbound (Since 773) {
            api []
            wire (Since 773) []
        }
