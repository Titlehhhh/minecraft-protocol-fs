namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ParticleStatus =

    let particleStatus =
        enumType "ParticleStatus" {
            values (Since 768) VarInt [ 0, "all"; 1, "decreased"; 2, "minimal" ]
        }
