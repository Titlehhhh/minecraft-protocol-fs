namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SelectKnownPacksServerbound =

    let selectKnownPacksServerbound =
        packet "SelectKnownPacksPacket" Configuration Serverbound (Since 766) {
            api [ field "Packs" (TArray(TNamed "KnownPack")) All ]
            wire (Since 766) [ read "packs" (Array(Named "KnownPack", VarIntCount)) "Packs" ]
        }
