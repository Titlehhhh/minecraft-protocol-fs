namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnInfo =

    let spawnInfo =
        namedType "SpawnInfo" {
            api [
                field "Dimension"        TInt                              (Since 766)
                field "Name"             TString                           (Since 766)
                field "HashedSeed"       TLong                             (Since 766)
                field "Gamemode"         (TEnum "Gamemode")                (Since 766)
                field "PreviousGamemode" TInt                              (Since 766)
                field "IsDebug"          TBool                             (Since 766)
                field "IsFlat"           TBool                             (Since 766)
                field "Death"            (TOption(TNamed "DeathLocation")) (Since 766)
                field "PortalCooldown"   TInt                              (Since 766)
                field "SeaLevel"         TInt                              (Since 768)
            ]

            wire (Between(766, 767)) [
                read "dimension"        VarInt                          "Dimension"
                read "name"             Str                             "Name"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         (enumAs "Gamemode" I8)          "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "death"            (Option(Named "DeathLocation")) "Death"
                read "portalCooldown"   VarInt                          "PortalCooldown"
            ]

            wire (Since 768) [
                read "dimension"        VarInt                          "Dimension"
                read "name"             Str                             "Name"
                read "hashedSeed"       I64                             "HashedSeed"
                read "gamemode"         (enumAs "Gamemode" I8)          "Gamemode"
                read "previousGamemode" U8                              "PreviousGamemode"
                read "isDebug"          Bool                            "IsDebug"
                read "isFlat"           Bool                            "IsFlat"
                read "death"            (Option(Named "DeathLocation")) "Death"
                read "portalCooldown"   VarInt                          "PortalCooldown"
                read "seaLevel"         VarInt                          "SeaLevel"
            ]
        }
