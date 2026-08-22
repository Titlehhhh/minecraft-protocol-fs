namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EnumShapeProbe =

    let enumShapeProbe =
        namedType "EnumShapeProbe" {
            api [
                field "Before" TInt                   (Since 766)
                field "Sound"  (TEnum "SoundSource")  (Since 766)
                field "Mode"   (TEnum "Gamemode")     (Since 766)
                field "Diff"   (TEnum "Difficulty")   (Since 766)
                field "After"  TInt                   (Since 766)
            ]

            wire (Between(766, 770)) [
                read "before" VarInt                    "Before"
                read "sound"  (enumOf "SoundSource")    "Sound"
                read "mode"   (enumAs "Gamemode" I8)    "Mode"
                read "diff"   (enumAs "Difficulty" U8)  "Diff"
                read "after"  VarInt                    "After"
            ]

            wire (Since 771) [
                read "before" VarInt                      "Before"
                read "sound"  (enumOf "SoundSource")      "Sound"
                read "mode"   (enumAs "Gamemode" VarInt)  "Mode"
                read "diff"   (enumOf "Difficulty")       "Diff"
                read "after"  VarInt                      "After"
            ]
        }
