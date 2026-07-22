namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateHealth =

    let updateHealth =
        packet "UpdateHealthPacket" Play Clientbound All {
            api [
                field "Health"         TFloat All
                field "Food"           TInt All
                field "FoodSaturation" TFloat All
            ]

            wire All [
                read "health"         F32    "Health"
                read "food"           VarInt "Food"
                read "foodSaturation" F32    "FoodSaturation"
            ]
        }
