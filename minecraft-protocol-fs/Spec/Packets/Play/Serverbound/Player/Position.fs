namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Position =

    let position =
        packet "PositionPacket" Play Serverbound All {
            api [
                field "X"        TDouble                  All
                field "Y"        TDouble                  All
                field "Z"        TDouble                  All
                field "OnGround" TBool                    (Until 767)
                field "Flags"    (TNamed "MovementFlags") (Since 768)
            ]

            wire (Until 767) [
                read "x"        F64  "X"
                read "y"        F64  "Y"
                read "z"        F64  "Z"
                read "onGround" Bool "OnGround"
            ]

            wire (Since 768) [
                read "x"     F64                     "X"
                read "y"     F64                     "Y"
                read "z"     F64                     "Z"
                read "flags" (Named "MovementFlags") "Flags"
            ]
        }
