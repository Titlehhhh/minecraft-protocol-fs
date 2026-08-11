namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBatchStart =

    let chunkBatchStart =
        packet "ChunkBatchStartPacket" Play Clientbound (Since 764) {
            api []
            wire (Since 764) []
        }
