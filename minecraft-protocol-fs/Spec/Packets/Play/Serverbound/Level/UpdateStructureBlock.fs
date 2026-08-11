namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateStructureBlock =

    let updateStructureBlock =
        packet "UpdateStructureBlockPacket" Play Serverbound All {
            api [
                field "Location"  (TNamed "Position") All
                field "Action"    TInt                All
                field "Mode"      TInt                All
                field "Name"      TString             All
                field "OffsetX"   TInt                All
                field "OffsetY"   TInt                All
                field "OffsetZ"   TInt                All
                field "SizeX"     TInt                All
                field "SizeY"     TInt                All
                field "SizeZ"     TInt                All
                field "Mirror"    TInt                All
                field "Rotation"  TInt                All
                field "Metadata"  TString             All
                field "Integrity" TFloat              All
                field "Seed"      TLong               All
                field "Flags"     TInt                All
            ]

            wire (Until 758) [
                read "location"  (Named "Position") "Location"
                read "action"    VarInt             "Action"
                read "mode"      VarInt             "Mode"
                read "name"      Str                "Name"
                read "offsetX"   I8                 "OffsetX"
                read "offsetY"   I8                 "OffsetY"
                read "offsetZ"   I8                 "OffsetZ"
                read "sizeX"     I8                 "SizeX"
                read "sizeY"     I8                 "SizeY"
                read "sizeZ"     I8                 "SizeZ"
                read "mirror"    VarInt             "Mirror"
                read "rotation"  VarInt             "Rotation"
                read "metadata"  Str                "Metadata"
                read "integrity" F32                "Integrity"
                read "seed"      VarLong            "Seed"
                read "flags"     U8                 "Flags"
            ]

            wire (Since 759) [
                read "location"  (Named "Position") "Location"
                read "action"    VarInt             "Action"
                read "mode"      VarInt             "Mode"
                read "name"      Str                "Name"
                read "offsetX"   I8                 "OffsetX"
                read "offsetY"   I8                 "OffsetY"
                read "offsetZ"   I8                 "OffsetZ"
                read "sizeX"     I8                 "SizeX"
                read "sizeY"     I8                 "SizeY"
                read "sizeZ"     I8                 "SizeZ"
                read "mirror"    VarInt             "Mirror"
                read "rotation"  VarInt             "Rotation"
                read "metadata"  Str                "Metadata"
                read "integrity" F32                "Integrity"
                read "seed"      VarInt             "Seed"
                read "flags"     U8                 "Flags"
            ]
        }
