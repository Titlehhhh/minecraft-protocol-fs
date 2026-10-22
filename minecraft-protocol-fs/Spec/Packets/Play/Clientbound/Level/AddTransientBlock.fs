namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AddTransientBlock =

    let addTransientBlock =
        packet "AddTransientBlockPacket" Play Clientbound (Since 777) {
            api
                [
                    field "Location" (TNamed "Position") All
                    field "Type" TInt All
                ]

            wire
                (Since 777)
                [
                    read "location" (Named "Position") "Location"
                    read "type" VarInt "Type"
                ]
        }
