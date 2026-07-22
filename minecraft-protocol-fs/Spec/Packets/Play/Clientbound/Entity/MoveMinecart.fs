namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MoveMinecart =

    let moveMinecart =
        packet "MoveMinecartPacket" Play Clientbound (Since 768) {
            api [
                field "EntityId" TInt All
                field "Steps"    (TArray(TNamed "MinecartStep")) All
            ]

            wire (Since 768) [
                read "entityId" VarInt "EntityId"
                read "steps"    (Array(Named "MinecartStep", VarIntCount)) "Steps"
            ]
        }
