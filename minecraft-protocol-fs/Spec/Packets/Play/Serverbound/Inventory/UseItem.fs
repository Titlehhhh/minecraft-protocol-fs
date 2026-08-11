namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UseItem =

    let useItem =
        packet "UseItemPacket" Play Serverbound All {
            api [
                field "Hand"     TInt             All
                field "Sequence" TInt             (Since 759)
                field "Rotation" (TNamed "Vec2f") (Since 767)
            ]

            wire (Until 758) [
                read "hand" VarInt "Hand"
            ]

            wire (Between(759, 766)) [
                read "hand"     VarInt "Hand"
                read "sequence" VarInt "Sequence"
            ]

            wire (Since 767) [
                read "hand"     VarInt          "Hand"
                read "sequence" VarInt          "Sequence"
                read "rotation" (Named "Vec2f") "Rotation"
            ]
        }
