namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MapPacket =

    let mapPacket =
        packet "MapPacket" Play Clientbound All {
            api [
                field "ItemDamage" TInt All
                field "Scale"      TInt All
                field "Locked"     TBool All
                field "Columns"    TInt All
                field "ColorData"  (TOption(TNamed "MapColorData")) All
            ]

            wire (Until 754) [
                read    "itemDamage"       VarInt "ItemDamage"
                read    "scale"            I8     "Scale"
                discard "trackingPosition" Bool
                read    "locked"           Bool   "Locked"
                discard "icons"            (Array(Named "MapIcon", VarIntCount))
                read    "columns"          I8     "Columns"

                ifNonZero "columns" [
                    readBlock (Named "MapColorData") "ColorData" [
                        read "rows" I8        "Rows"
                        read "x"    I8        "X"
                        read "y"    I8        "Y"
                        read "data" ByteArray "Data"
                    ]
                ]
            ]

            wire (Since 765) [
                read    "itemDamage" VarInt "ItemDamage"
                read    "scale"      I8     "Scale"
                read    "locked"     Bool   "Locked"
                discard "icons"      (Option(Array(Named "MapIcon", VarIntCount)))
                read    "columns"    U8     "Columns"

                ifNonZero "columns" [
                    readBlock (Named "MapColorData") "ColorData" [
                        read "rows" I8        "Rows"
                        read "x"    I8        "X"
                        read "y"    I8        "Y"
                        read "data" ByteArray "Data"
                    ]
                ]
            ]
        }
