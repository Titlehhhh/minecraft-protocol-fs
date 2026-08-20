namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module InteractAction =

    let interactAction =
        unionType "InteractAction" {
            cases
                (Until 774)
                [
                    case1 0 "Interact" [ read "hand" VarInt "Hand" ]

                    case1 1 "Attack" []

                    case1
                        2
                        "InteractAt"
                        [
                            read "x" F32 "X"
                            read "y" F32 "Y"
                            read "z" F32 "Z"
                            read "hand" VarInt "Hand"
                        ]
                ]
        }
