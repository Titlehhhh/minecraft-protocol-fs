namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Respawn =

    let respawn =
        packet "RespawnPacket" Play Clientbound All {
            api [
                field "Dimension"        TString                           (Until 736)
                field "DimensionNbt"     TNbt                              (Between(751, 758))
                field "DimensionName"    TString                           (Between(759, 765))
                field "WorldName"        TString                           (Until 765)
                field "HashedSeed"       TLong                             (Until 765)
                field "Gamemode"         TInt                              (Until 765)
                field "PreviousGamemode" TInt                              (Until 765)
                field "IsDebug"          TBool                             (Until 765)
                field "IsFlat"           TBool                             (Until 765)
                field "Death"            (TOption(TNamed "DeathLocation")) (Between(759, 765))
                field "PortalCooldown"   TInt                              (Between(763, 765))
                field "CopyMetadata"     TBool                             All
                field "WorldState"       (TNamed "SpawnInfo")              (Since 766)
            ]

            wire (Until 736) [
                read "dimension"        Str  "Dimension"
                read "worldName"        Str  "WorldName"
                read "hashedSeed"       I64  "HashedSeed"
                read "gamemode"         U8   "Gamemode"
                read "previousGamemode" U8   "PreviousGamemode"
                read "isDebug"          Bool "IsDebug"
                read "isFlat"           Bool "IsFlat"
                read "copyMetadata"     Bool "CopyMetadata"
            ]

            wire (Between(751, 758)) [
                read "dimension"        Nbt  "DimensionNbt"
                read "worldName"        Str  "WorldName"
                read "hashedSeed"       I64  "HashedSeed"
                read "gamemode"         U8   "Gamemode"
                read "previousGamemode" U8   "PreviousGamemode"
                read "isDebug"          Bool "IsDebug"
                read "isFlat"           Bool "IsFlat"
                read "copyMetadata"     Bool "CopyMetadata"
            ]

            wire (Between(759, 759)) [
                read "dimension"        Str                             "DimensionName"
                read "worldName"        Str                             "WorldName"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         U8                              "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "copyMetadata"     Bool                            "CopyMetadata"
                read "death"            (Option(Named "DeathLocation")) "Death"
            ]

            wire (Between(760, 762)) [
                read "dimension"        Str                             "DimensionName"
                read "worldName"        Str                             "WorldName"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         I8                              "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "copyMetadata"     Bool                            "CopyMetadata"
                read "death"            (Option(Named "DeathLocation")) "Death"
            ]

            wire (Between(763, 763)) [
                read "dimension"        Str                             "DimensionName"
                read "worldName"        Str                             "WorldName"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         I8                              "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "copyMetadata"     Bool                            "CopyMetadata"
                read "death"            (Option(Named "DeathLocation")) "Death"
                read "portalCooldown"   VarInt                          "PortalCooldown"
            ]

            wire (Between(764, 765)) [
                read "dimension"        Str                             "DimensionName"
                read "worldName"        Str                             "WorldName"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         I8                              "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "death"            (Option(Named "DeathLocation")) "Death"
                read "portalCooldown"   VarInt                          "PortalCooldown"
                read "copyMetadata"     Bool                            "CopyMetadata"
            ]

            wire (Since 766) [
                read "worldState"   (Named "SpawnInfo") "WorldState"
                read "copyMetadata" U8                   "CopyMetadata"
            ]
        }
