namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module NamedSoundEffect =

    let namedSoundEffect =
        packet "NamedSoundEffectPacket" Play Clientbound (Until 760) {
            api [
                field "SoundName"     TString All
                field "SoundCategory" TInt    All
                field "X"             TInt    All
                field "Y"             TInt    All
                field "Z"             TInt    All
                field "Volume"        TFloat  All
                field "Pitch"         TFloat  All
                field "Seed"          TLong   (Between(759, 760))
            ]

            wire (Until 758) [
                read "soundName"     Str    "SoundName"
                read "soundCategory" VarInt "SoundCategory"
                read "x"             I32    "X"
                read "y"             I32    "Y"
                read "z"             I32    "Z"
                read "volume"        F32    "Volume"
                read "pitch"         F32    "Pitch"
            ]

            wire (Between(759, 760)) [
                read "soundName"     Str    "SoundName"
                read "soundCategory" VarInt "SoundCategory"
                read "x"             I32    "X"
                read "y"             I32    "Y"
                read "z"             I32    "Z"
                read "volume"        F32    "Volume"
                read "pitch"         F32    "Pitch"
                read "seed"          I64    "Seed"
            ]
        }
