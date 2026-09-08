namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CombatEventAction =

    let combatEventAction =
        unionType "CombatEventAction" {
            cases
                (Until 754)
                [
                    case1 0 "Enter" []

                    case1 1 "End" [ read "duration" VarInt "Duration"; read "entityId" I32 "EntityId" ]

                    case1
                        2
                        "Death"
                        [
                            read "playerId" VarInt "PlayerId"
                            read "entityId" I32 "EntityId"
                            read "message" Str "MessageJson"
                        ]
                ]
        }
