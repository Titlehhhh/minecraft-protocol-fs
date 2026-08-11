namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateCommandBlockMinecart =

    let updateCommandBlockMinecart =
        packet "UpdateCommandBlockMinecartPacket" Play Serverbound All {
            api [
                field "EntityId"    TInt    All
                field "Command"     TString All
                field "TrackOutput" TBool   All
            ]

            wire All [
                read "entityId"    VarInt "EntityId"
                read "command"     Str    "Command"
                read "trackOutput" Bool   "TrackOutput"
            ]
        }
