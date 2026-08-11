namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityDestroy =

    let entityDestroy =
        packet "EntityDestroyPacket" Play Clientbound All {
            api [
                field "EntityIds" (TArray TInt) All
            ]

            wire (Until 754) [
                read "entityIds" (Array(VarInt, VarIntCount)) "EntityIds"
            ]

            wire (Since 756) [
                read "entityIds" (Array(VarInt, VarIntCount)) "EntityIds"
            ]
        }
