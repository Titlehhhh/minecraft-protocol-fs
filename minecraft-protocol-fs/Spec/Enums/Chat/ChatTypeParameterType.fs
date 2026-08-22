namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatTypeParameterType =

    let chatTypeParameterType =
        enumType "ChatTypeParameterType" {
            values (Since 766) VarInt [ 0, "content"; 1, "sender"; 2, "target" ]
        }
