namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginAcknowledged =

    let loginAcknowledged =
        packet "LoginAcknowledgedPacket" Login Serverbound (Since 764) {
            api []

            wire (Since 764) []
        }
