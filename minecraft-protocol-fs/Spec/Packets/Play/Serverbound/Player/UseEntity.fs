namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UseEntity =

    let useEntity =
        packet "UseEntityPacket" Play Serverbound All {
            api
                [
                    field "Target" TInt All
                    field "Action" (TUnion "InteractAction") (Until 774)
                    field "Hand" TInt (Since 775)
                    field "Location" (TNamed "LpVec3") (Since 775)
                    field "Sneaking" TBool All
                ]

            wire
                (Until 774)
                [
                    read "target" VarInt "Target"
                    read "mouse" VarInt "_mouse"
                    readUnion "_mouse" "InteractAction" "Action"
                    read "sneaking" Bool "Sneaking"
                ]

            wire
                (Since 775)
                [
                    read "target" VarInt "Target"
                    read "hand" VarInt "Hand"
                    read "location" (Named "LpVec3") "Location"
                    read "sneaking" Bool "Sneaking"
                ]
        }
