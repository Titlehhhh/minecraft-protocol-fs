namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AbilitiesServerbound =

    let abilitiesServerbound =
        packet "AbilitiesPacket" Play Serverbound All {
            api [
                field "Flags" TInt All
            ]

            wire All [
                read "flags" I8 "Flags"
            ]
        }
