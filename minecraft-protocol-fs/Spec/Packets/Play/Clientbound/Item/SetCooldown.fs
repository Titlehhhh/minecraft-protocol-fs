namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetCooldown =

    let setCooldown =
        packet "SetCooldownPacket" Play Clientbound All {
            api [
                field "ItemId"        TInt    (Until 767)
                field "CooldownGroup" TString (Since 768)
                field "CooldownTicks" TInt    All
            ]

            wire (Until 767) [
                read "itemID"        VarInt "ItemId"
                read "cooldownTicks" VarInt "CooldownTicks"
            ]

            wire (Since 768) [
                read "cooldownGroup" Str    "CooldownGroup"
                read "cooldownTicks" VarInt "CooldownTicks"
            ]
        }
