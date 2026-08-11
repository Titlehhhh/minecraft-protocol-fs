namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBatchReceived =

    let chunkBatchReceived =
        packet "ChunkBatchReceivedPacket" Play Serverbound (Since 764) {
            api [
                field "ChunksPerTick" TFloat All
            ]

            wire (Since 764) [
                read "chunksPerTick" F32 "ChunksPerTick"
            ]
        }
