namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Gamemode =

    let gamemode =
        enumType "Gamemode" {
            values (Since 766) I8 [ 0, "survival"; 1, "creative"; 2, "adventure"; 3, "spectator" ]
        }
