namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module NameItem =

    let nameItem =
        packet "NameItemPacket" Play Serverbound All {
            api [
                field "Name" TString All
            ]

            wire All [
                read "name" Str "Name"
            ]
        }
