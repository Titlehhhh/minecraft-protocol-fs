namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DebugSample =

    let debugSample =
        packet "DebugSamplePacket" Play Clientbound (Since 766) {
            api [
                field "Sample" (TArray TLong) All
                field "Type"   TInt           All
            ]

            wire (Since 766) [
                read "sample" (Array(I64, VarIntCount)) "Sample"
                read "type"   VarInt                    "Type"
            ]
        }
