namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Flying =

    let flying =
        packet "FlyingPacket" Play Serverbound All {
            api [
                field "OnGround" TBool                    (Until 767)
                field "Flags"    (TNamed "MovementFlags") (Since 768)
            ]

            wire (Until 767) [
                read "onGround" Bool "OnGround"
            ]

            wire (Since 768) [
                read "flags" (Named "MovementFlags") "Flags"
            ]
        }
