namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpectateEntity =

    let spectateEntity =
        packet "SpectateEntityPacket" Play Serverbound (Between(775, 776)) {
            protoId "spectate_entity"
            protoAlias "spectator_action"

            api [ field "EntityId" TInt All ]

            wire (Between(775, 776)) [ read "entityId" VarInt "EntityId" ]
        }
