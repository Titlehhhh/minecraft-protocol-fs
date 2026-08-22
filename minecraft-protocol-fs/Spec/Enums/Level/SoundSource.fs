namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SoundSource =

    let soundSource =
        enumType "SoundSource" {
            values (Between(761, 770)) VarInt [
                0,  "master"
                1,  "music"
                2,  "record"
                3,  "weather"
                4,  "block"
                5,  "hostile"
                6,  "neutral"
                7,  "player"
                8,  "ambient"
                9,  "voice"
            ]

            values (Since 771) VarInt [
                0,  "master"
                1,  "music"
                2,  "record"
                3,  "weather"
                4,  "block"
                5,  "hostile"
                6,  "neutral"
                7,  "player"
                8,  "ambient"
                9,  "voice"
                10, "ui"
            ]
        }
