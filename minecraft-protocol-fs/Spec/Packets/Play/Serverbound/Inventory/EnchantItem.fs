namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EnchantItem =

    let enchantItem =
        packet "EnchantItemPacket" Play Serverbound All {
            api [ field "WindowId" TInt All; field "Enchantment" TInt All ]

            wire (Until 766) [ read "windowId" I8 "WindowId"; read "enchantment" I8 "Enchantment" ]

            wire (Between(767, 767)) [ read "windowId" U8 "WindowId"; read "enchantment" VarInt "Enchantment" ]

            wire (Since 768) [ read "windowId" VarInt "WindowId"; read "enchantment" VarInt "Enchantment" ]
        }
