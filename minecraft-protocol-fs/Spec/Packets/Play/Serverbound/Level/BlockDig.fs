namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BlockDig =

    let blockDig =
        packet "BlockDigPacket" Play Serverbound All {
            api [
                field "Status"   TInt                All
                field "Location" (TNamed "Position") All
                field "Face"     TInt                All
                field "Sequence" TInt                (Since 759)
            ]

            wire (Until 758) [
                read "status"   VarInt             "Status"
                read "location" (Named "Position") "Location"
                read "face"     I8                 "Face"
            ]

            wire (Since 759) [
                read "status"   VarInt             "Status"
                read "location" (Named "Position") "Location"
                read "face"     I8                 "Face"
                read "sequence" VarInt             "Sequence"
            ]
        }
