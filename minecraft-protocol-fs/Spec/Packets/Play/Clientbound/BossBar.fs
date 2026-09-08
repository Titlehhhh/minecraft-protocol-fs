namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BossBar =

    let bossBarPacket =
        packet "BossBarPacket" Play Clientbound All {
            api [
                field "EntityUuid" TUuid All
                field "Action"     (TUnion "BossBarAction") All
            ]

            wire All [
                read      "entityUUID" Uuid   "EntityUuid"
                read      "action"     VarInt "_action"
                readUnion "_action" "BossBarAction" "Action"
            ]
        }
