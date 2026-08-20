namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginPlay =

    let loginPlay =
        packet "LoginPacket" Play Clientbound All {
            api
                [
                    field "EntityId" TInt All
                    field "IsHardcore" TBool (Since 751)
                    field "Gamemode" TInt (Until 765)
                    field "PreviousGamemode" TInt (Until 765)
                    field "WorldNames" (TArray TString) All
                    field "DimensionCodec" TNbt (Until 763)
                    field "Dimension" TString (Until 736)
                    field "DimensionNbt" TNbt (Between(751, 758))
                    field "WorldType" TString (Between(759, 765))
                    field "WorldName" TString (Until 765)
                    field "HashedSeed" TLong (Until 765)
                    field "MaxPlayers" TInt All
                    field "ViewDistance" TInt All
                    field "SimulationDistance" TInt (Since 757)
                    field "ReducedDebugInfo" TBool All
                    field "EnableRespawnScreen" TBool All
                    field "IsDebug" TBool (Until 765)
                    field "IsFlat" TBool (Until 765)
                    field "Death" (TOption(TNamed "DeathLocation")) (Between(759, 765))
                    field "PortalCooldown" TInt (Between(763, 765))
                    field "DoLimitedCrafting" TBool (Since 764)
                    field "WorldState" (TNamed "SpawnInfo") (Since 766)
                    field "OnlineMode" TBool (Since 776)
                    field "EnforcesSecureChat" TBool (Since 766)
                ]

            wire
                (Until 736)
                [
                    read "entityId" I32 "EntityId"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" U8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "dimension" Str "Dimension"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" U8 "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                ]

            wire
                (Between(751, 754))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" U8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "dimension" Nbt "DimensionNbt"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                ]

            wire
                (Between(755, 756))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" I8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "dimension" Nbt "DimensionNbt"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                ]

            wire
                (Between(757, 758))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" I8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "dimension" Nbt "DimensionNbt"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                ]

            wire
                (Between(759, 762))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" I8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "worldType" Str "WorldType"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                    read "death" (Option(Named "DeathLocation")) "Death"
                ]

            wire
                (Between(763, 763))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" I8 "PreviousGamemode"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "dimensionCodec" Nbt "DimensionCodec"
                    read "worldType" Str "WorldType"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                    read "death" (Option(Named "DeathLocation")) "Death"
                    read "portalCooldown" VarInt "PortalCooldown"
                ]

            wire
                (Between(764, 765))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "doLimitedCrafting" Bool "DoLimitedCrafting"
                    read "worldType" Str "WorldType"
                    read "worldName" Str "WorldName"
                    read "hashedSeed" I64 "HashedSeed"
                    read "gameMode" U8 "Gamemode"
                    read "previousGameMode" I8 "PreviousGamemode"
                    read "isDebug" Bool "IsDebug"
                    read "isFlat" Bool "IsFlat"
                    read "death" (Option(Named "DeathLocation")) "Death"
                    read "portalCooldown" VarInt "PortalCooldown"
                ]

            wire
                (Between(766, 775))
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "doLimitedCrafting" Bool "DoLimitedCrafting"
                    read "worldState" (Named "SpawnInfo") "WorldState"
                    read "enforcesSecureChat" Bool "EnforcesSecureChat"
                ]

            wire
                (Since 776)
                [
                    read "entityId" I32 "EntityId"
                    read "isHardcore" Bool "IsHardcore"
                    read "worldNames" (Array(Str, VarIntCount)) "WorldNames"
                    read "maxPlayers" VarInt "MaxPlayers"
                    read "viewDistance" VarInt "ViewDistance"
                    read "simulationDistance" VarInt "SimulationDistance"
                    read "reducedDebugInfo" Bool "ReducedDebugInfo"
                    read "enableRespawnScreen" Bool "EnableRespawnScreen"
                    read "doLimitedCrafting" Bool "DoLimitedCrafting"
                    read "worldState" (Named "SpawnInfo") "WorldState"
                    read "onlineMode" Bool "OnlineMode"
                    read "enforcesSecureChat" Bool "EnforcesSecureChat"
                ]
        }
