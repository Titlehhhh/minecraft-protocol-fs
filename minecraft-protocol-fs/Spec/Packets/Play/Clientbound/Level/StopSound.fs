namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module StopSound =

    let stopSound =
        packet "StopSoundPacket" Play Clientbound All {
            api [
                field "Flags"  TInt All
                field "Source" (TOption TInt) All
                field "Sound"  (TOption TString) All
            ]

            wire All [
                read    "flags" I8 "Flags"
                readOpt "source" VarInt "Source" "flags" [ 1; 3 ]
                readOpt "sound"  Str    "Sound" "flags" [ 2; 3 ]
            ]
        }
