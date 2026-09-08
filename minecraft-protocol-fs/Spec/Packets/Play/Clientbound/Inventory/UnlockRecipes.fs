namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UnlockRecipes =

    let unlockRecipes =
        packet "UnlockRecipesPacket" Play Clientbound (Between(735, 767)) {
            api [
                field "Action"                TInt All
                field "CraftingBookOpen"      TBool All
                field "FilteringCraftable"    TBool All
                field "SmeltingBookOpen"      TBool All
                field "FilteringSmeltable"    TBool All
                field "BlastFurnaceOpen"      TBool (Between(751, 767))
                field "FilteringBlastFurnace" TBool (Between(751, 767))
                field "SmokerBookOpen"        TBool (Between(751, 767))
                field "FilteringSmoker"       TBool (Between(751, 767))
                field "Recipes1"              (TArray TString) All
                field "Recipes2"              (TOption(TArray TString)) All
            ]

            wire (Between(735, 736)) [
                read    "action"             VarInt "Action"
                read    "craftingBookOpen"   Bool   "CraftingBookOpen"
                read    "filteringCraftable" Bool   "FilteringCraftable"
                read    "smeltingBookOpen"   Bool   "SmeltingBookOpen"
                read    "filteringSmeltable" Bool   "FilteringSmeltable"
                read    "recipes1" (Array(Str, VarIntCount)) "Recipes1"
                readOpt "recipes2" (Array(Str, VarIntCount)) "Recipes2" "action" [ 0 ]
            ]

            wire (Between(751, 767)) [
                read    "action"                VarInt "Action"
                read    "craftingBookOpen"      Bool   "CraftingBookOpen"
                read    "filteringCraftable"    Bool   "FilteringCraftable"
                read    "smeltingBookOpen"      Bool   "SmeltingBookOpen"
                read    "filteringSmeltable"    Bool   "FilteringSmeltable"
                read    "blastFurnaceOpen"      Bool   "BlastFurnaceOpen"
                read    "filteringBlastFurnace" Bool   "FilteringBlastFurnace"
                read    "smokerBookOpen"        Bool   "SmokerBookOpen"
                read    "filteringSmoker"       Bool   "FilteringSmoker"
                read    "recipes1" (Array(Str, VarIntCount)) "Recipes1"
                readOpt "recipes2" (Array(Str, VarIntCount)) "Recipes2" "action" [ 0 ]
            ]
        }
