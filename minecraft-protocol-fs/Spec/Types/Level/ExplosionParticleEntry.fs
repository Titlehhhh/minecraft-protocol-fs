namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ExplosionParticleEntry =

    let explosionParticleEntry =
        record "ExplosionParticleEntry" (Since 773) [ col "data" (Named "ExplosionParticleInfo"); col "weight" VarInt ]
