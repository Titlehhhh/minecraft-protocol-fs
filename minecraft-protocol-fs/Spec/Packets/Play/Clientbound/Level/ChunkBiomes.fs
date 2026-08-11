namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBiomes =

    let chunkBiomes =
        packet "ChunkBiomesPacket" Play Clientbound (Since 762) {
            api [ field "Biomes" (TArray(TNamed "ChunkBiomeData")) All ]
            wire (Since 762) [ read "biomes" (Array(Named "ChunkBiomeData", VarIntCount)) "Biomes" ]
        }
