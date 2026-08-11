namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBatchFinished =

    let chunkBatchFinished =
        packet "ChunkBatchFinishedPacket" Play Clientbound (Since 764) {
            api [
                field "BatchSize" TInt All
            ]

            wire (Since 764) [
                read "batchSize" VarInt "BatchSize"
            ]
        }
