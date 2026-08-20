namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ExplosionParticleInfo =

    let explosionParticleInfo =
        record
            "ExplosionParticleInfo"
            (Since 773)
            [ col "particle" (Named "Particle"); col "scaling" F32; col "speed" F32 ]
