namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Explosion =

    let explosion =
        packet "ExplosionPacket" Play Clientbound All {
            api
                [
                    field "X" TDouble (Until 772)
                    field "Y" TDouble (Until 772)
                    field "Z" TDouble (Until 772)
                    field "Radius" TFloat (Until 767)
                    field "AffectedBlockOffsets" (TArray(TNamed "ExplosionBlockOffset")) (Until 767)
                    field "PlayerMotionX" TFloat (Until 767)
                    field "PlayerMotionY" TFloat (Until 767)
                    field "PlayerMotionZ" TFloat (Until 767)
                    field "BlockInteractionType" TInt (Between(765, 767))
                    field "SmallExplosionParticle" (TNamed "Particle") (Between(765, 767))
                    field "LargeExplosionParticle" (TNamed "Particle") (Between(765, 767))
                    field "Sound" (THolder(TNamed "ItemSoundEvent")) (Since 765)
                    field "PlayerKnockback" (TOption(TNamed "Vec3f64")) (Since 768)
                    field "ExplosionParticle" (TNamed "Particle") (Since 768)
                    field "Center" (TNamed "Vec3f64") (Since 773)
                    field "BlockCount" TInt (Since 773)
                    field "BlockParticles" (TArray(TNamed "ExplosionParticleEntry")) (Since 773)
                ]

            wire
                (Until 754)
                [
                    read "x" F32 "X"
                    read "y" F32 "Y"
                    read "z" F32 "Z"
                    read "radius" F32 "Radius"
                    read
                        "affectedBlockOffsets"
                        (Array(Named "ExplosionBlockOffset", TypedCount I32))
                        "AffectedBlockOffsets"
                    read "playerMotionX" F32 "PlayerMotionX"
                    read "playerMotionY" F32 "PlayerMotionY"
                    read "playerMotionZ" F32 "PlayerMotionZ"
                ]

            wire
                (Between(755, 760))
                [
                    read "x" F32 "X"
                    read "y" F32 "Y"
                    read "z" F32 "Z"
                    read "radius" F32 "Radius"
                    read
                        "affectedBlockOffsets"
                        (Array(Named "ExplosionBlockOffset", VarIntCount))
                        "AffectedBlockOffsets"
                    read "playerMotionX" F32 "PlayerMotionX"
                    read "playerMotionY" F32 "PlayerMotionY"
                    read "playerMotionZ" F32 "PlayerMotionZ"
                ]

            wire
                (Between(761, 764))
                [
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "radius" F32 "Radius"
                    read
                        "affectedBlockOffsets"
                        (Array(Named "ExplosionBlockOffset", VarIntCount))
                        "AffectedBlockOffsets"
                    read "playerMotionX" F32 "PlayerMotionX"
                    read "playerMotionY" F32 "PlayerMotionY"
                    read "playerMotionZ" F32 "PlayerMotionZ"
                ]

            wire
                (Between(765, 767))
                [
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "radius" F32 "Radius"
                    read
                        "affectedBlockOffsets"
                        (Array(Named "ExplosionBlockOffset", VarIntCount))
                        "AffectedBlockOffsets"
                    read "playerMotionX" F32 "PlayerMotionX"
                    read "playerMotionY" F32 "PlayerMotionY"
                    read "playerMotionZ" F32 "PlayerMotionZ"
                    read "block_interaction_type" VarInt "BlockInteractionType"
                    read "small_explosion_particle" (Named "Particle") "SmallExplosionParticle"
                    read "large_explosion_particle" (Named "Particle") "LargeExplosionParticle"
                    read "sound" (RegistryHolder(Named "ItemSoundEvent")) "Sound"
                ]

            wire
                (Between(768, 768))
                [
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "playerKnockback" (Option(Named "Vec3f")) "PlayerKnockback"
                    read "explosionParticle" (Named "Particle") "ExplosionParticle"
                    read "sound" (RegistryHolder(Named "ItemSoundEvent")) "Sound"
                ]

            wire
                (Between(769, 772))
                [
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "playerKnockback" (Option(Named "Vec3f64")) "PlayerKnockback"
                    read "explosionParticle" (Named "Particle") "ExplosionParticle"
                    read "sound" (RegistryHolder(Named "ItemSoundEvent")) "Sound"
                ]

            wire
                (Since 773)
                [
                    read "center" (Named "Vec3f64") "Center"
                    read "radius" F32 "Radius"
                    read "blockCount" I32 "BlockCount"
                    read "playerKnockback" (Option(Named "Vec3f64")) "PlayerKnockback"
                    read "explosionParticle" (Named "Particle") "ExplosionParticle"
                    read "sound" (RegistryHolder(Named "ItemSoundEvent")) "Sound"
                    read "blockParticles" (Array(Named "ExplosionParticleEntry", VarIntCount)) "BlockParticles"
                ]
        }
