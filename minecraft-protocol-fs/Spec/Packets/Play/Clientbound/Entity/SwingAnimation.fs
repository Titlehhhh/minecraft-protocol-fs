namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SwingAnimation =

    let swingAnimation =
        packet "SwingAnimationPacket" Play Clientbound (Since 777) {
            api
                [
                    field "EntityId" TInt All
                    field "Hand" TInt All
                    field "AnimationType" TInt All
                    field "Duration" TInt All
                ]

            wire
                (Since 777)
                [
                    read "entityId" VarInt "EntityId"
                    read "hand" VarInt "Hand"
                    read "animationType" VarInt "AnimationType"
                    read "duration" VarInt "Duration"
                ]
        }
