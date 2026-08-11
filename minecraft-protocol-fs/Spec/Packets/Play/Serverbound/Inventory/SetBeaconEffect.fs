namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetBeaconEffect =

    let setBeaconEffect =
        packet "SetBeaconEffectPacket" Play Serverbound All {
            api [
                field "PrimaryEffect"   (TOption TInt) All
                field "SecondaryEffect" (TOption TInt) All
            ]

            wire (Until 758) [
                read "primaryEffect"   VarInt "PrimaryEffect"
                read "secondaryEffect" VarInt "SecondaryEffect"
            ]

            wire (Since 759) [
                read "primaryEffect"   (Option VarInt) "PrimaryEffect"
                read "secondaryEffect" (Option VarInt) "SecondaryEffect"
            ]
        }
