namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBiomeData =

    let chunkBiomeData =
        record "ChunkBiomeData" (Since 762) [
            col "position" (Named "PackedChunkPos")
            col "data"     ByteArray
        ]
