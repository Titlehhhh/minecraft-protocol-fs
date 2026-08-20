namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpectateEntity =

    let spectateEntity =
        packet "SpectateEntityPacket" Play Serverbound (Since 775) {
            api [ field "EntityId" TInt All ]

            wire (Since 775) [ read "entityId" VarInt "EntityId" ]
        }
