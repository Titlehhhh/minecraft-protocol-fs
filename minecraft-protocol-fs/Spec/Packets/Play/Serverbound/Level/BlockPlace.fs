namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BlockPlace =

    let blockPlace =
        packet "BlockPlacePacket" Play Serverbound All {
            api [
                field "Hand"           TInt                All
                field "Location"       (TNamed "Position") All
                field "Direction"      TInt                All
                field "CursorX"        TFloat              All
                field "CursorY"        TFloat              All
                field "CursorZ"        TFloat              All
                field "InsideBlock"    TBool               All
                field "WorldBorderHit" TBool               (Since 768)
                field "Sequence"       TInt                (Since 759)
            ]

            wire (Until 758) [
                read "hand"        VarInt             "Hand"
                read "location"    (Named "Position") "Location"
                read "direction"   VarInt             "Direction"
                read "cursorX"     F32                "CursorX"
                read "cursorY"     F32                "CursorY"
                read "cursorZ"     F32                "CursorZ"
                read "insideBlock" Bool               "InsideBlock"
            ]

            wire (Between(759, 767)) [
                read "hand"        VarInt             "Hand"
                read "location"    (Named "Position") "Location"
                read "direction"   VarInt             "Direction"
                read "cursorX"     F32                "CursorX"
                read "cursorY"     F32                "CursorY"
                read "cursorZ"     F32                "CursorZ"
                read "insideBlock" Bool               "InsideBlock"
                read "sequence"    VarInt             "Sequence"
            ]

            wire (Since 768) [
                read "hand"           VarInt             "Hand"
                read "location"       (Named "Position") "Location"
                read "direction"      VarInt             "Direction"
                read "cursorX"        F32                "CursorX"
                read "cursorY"        F32                "CursorY"
                read "cursorZ"        F32                "CursorZ"
                read "insideBlock"    Bool               "InsideBlock"
                read "worldBorderHit" Bool               "WorldBorderHit"
                read "sequence"       VarInt             "Sequence"
            ]
        }
