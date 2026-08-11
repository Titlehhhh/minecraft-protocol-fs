namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateViewPosition =

    let updateViewPosition =
        packet "UpdateViewPositionPacket" Play Clientbound All {
            api [
                field "ChunkX" TInt All
                field "ChunkZ" TInt All
            ]

            wire All [
                read "chunkX" VarInt "ChunkX"
                read "chunkZ" VarInt "ChunkZ"
            ]
        }
