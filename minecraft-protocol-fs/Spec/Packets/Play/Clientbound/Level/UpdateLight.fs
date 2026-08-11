namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateLight =

    let updateLight =
        packet "UpdateLightPacket" Play Clientbound All {
            api [
                field "ChunkX"                    TInt                  All
                field "ChunkZ"                    TInt                  All
                field "TrustEdges"                TBool                 (Until 762)
                field "SkyLightMaskLegacy"        TInt                  (Until 754)
                field "BlockLightMaskLegacy"      TInt                  (Until 754)
                field "EmptySkyLightMaskLegacy"   TInt                  (Until 754)
                field "EmptyBlockLightMaskLegacy" TInt                  (Until 754)
                field "Data"                      TBytes                (Until 754)
                field "SkyLightMask"              (TArray TLong)        (Since 755)
                field "BlockLightMask"            (TArray TLong)        (Since 755)
                field "EmptySkyLightMask"         (TArray TLong)        (Since 755)
                field "EmptyBlockLightMask"       (TArray TLong)        (Since 755)
                field "SkyLight"                  (TArray(TArray TInt)) (Since 755)
                field "BlockLight"                (TArray(TArray TInt)) (Since 755)
            ]

            wire (Until 754) [
                read "chunkX"              VarInt    "ChunkX"
                read "chunkZ"              VarInt    "ChunkZ"
                read "trustEdges"          Bool      "TrustEdges"
                read "skyLightMask"        VarInt    "SkyLightMaskLegacy"
                read "blockLightMask"      VarInt    "BlockLightMaskLegacy"
                read "emptySkyLightMask"   VarInt    "EmptySkyLightMaskLegacy"
                read "emptyBlockLightMask" VarInt    "EmptyBlockLightMaskLegacy"
                read "data"                RestBytes "Data"
            ]

            wire (Between(755, 762)) [
                read "chunkX"              VarInt                                       "ChunkX"
                read "chunkZ"              VarInt                                       "ChunkZ"
                read "trustEdges"          Bool                                         "TrustEdges"
                read "skyLightMask"        (Array(I64, VarIntCount))                    "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                    "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                    "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                    "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount)) "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount)) "BlockLight"
            ]

            wire (Since 763) [
                read "chunkX"              VarInt                                       "ChunkX"
                read "chunkZ"              VarInt                                       "ChunkZ"
                read "skyLightMask"        (Array(I64, VarIntCount))                    "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                    "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                    "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                    "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount)) "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount)) "BlockLight"
            ]
        }
