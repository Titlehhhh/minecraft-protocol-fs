namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UnloadChunk =

    let unloadChunk =
        packet "UnloadChunkPacket" Play Clientbound All {
            api [
                field "ChunkX" TInt All
                field "ChunkZ" TInt All
            ]

            wire (Until 763) [
                read "x" I32 "ChunkX"
                read "z" I32 "ChunkZ"
            ]

            wire (Since 764) [
                read "z" I32 "ChunkZ"
                read "x" I32 "ChunkX"
            ]
        }
